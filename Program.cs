using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

class Program
{
	private static readonly ActivitySource ActivitySource = new ActivitySource("MyCompany.MyProduct.MyLibrary");

	static async Task Main(string[] args)
	{
		// OTLP Exporter Endpoint for Honeycomb
		string honeycombOtlpEndpoint = "https://api.honeycomb.io:443";
		string honeycombApiKey = "YOUR API KEY;
		string sqsQueueUrl = "YOUR SQS QUEUE URL";

		// Setup OpenTelemetry with OTLP Exporter
		using var tracerProvider = Sdk.CreateTracerProviderBuilder()
			.AddSource("MyCompany.MyProduct.MyLibrary")
			.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyConsoleApp"))
			.AddConsoleExporter()  // Optional: Keeps the console exporter
			.AddOtlpExporter(otlpOptions =>
			{
				otlpOptions.Endpoint = new Uri(honeycombOtlpEndpoint);
				otlpOptions.Headers = $"x-honeycomb-team={honeycombApiKey}";
			})
			.Build();

		// Simplified test to see if activity can be created
		using (var activity = ActivitySource.StartActivity("TestActivity"))
		{
			if (activity == null)
			{
				Console.WriteLine("Failed to create an activity.");
			}
			else
			{
				Console.WriteLine($"Created activity with TraceId: {activity.TraceId}");

				// Serialize the current context and include it in the message body
				var contextData = new
				{
					TraceId = activity.Context.TraceId.ToString(),
					SpanId = activity.Context.SpanId.ToString(),
					TraceFlags = activity.Context.TraceFlags.ToString(),
					Message = "Test message sent to SQS"
				};

				string messageBody = JsonSerializer.Serialize(contextData);

				// Send a message to SQS with the serialized context
				await SendMessageToSqsAsync(sqsQueueUrl, messageBody);

				// Receive a message from SQS and continue the trace
				await ReceiveMessageFromSqsAsync(sqsQueueUrl);
			}
		}

		// Keep the console window open
		Console.WriteLine("Press any key to exit...");
		Console.ReadKey();
	}

	private static async Task SendMessageToSqsAsync(string queueUrl, string messageBody)
	{
		// Start a new span for the SQS send operation
		using (var sqsSpan = ActivitySource.StartActivity("SendMessageToSqsAsync"))
		{
			if (sqsSpan != null)
			{
				// Add the SQS Queue URL as an attribute to the span
				sqsSpan.SetTag("request.url", queueUrl);
			}

			// Initialize the SQS client with hardcoded credentials
			var sqsClient = new AmazonSQSClient("YOUR KEY", "YOUR SECRET", Amazon.RegionEndpoint.USEast1);

			var sendMessageRequest = new SendMessageRequest
			{
				QueueUrl = queueUrl,
				MessageBody = messageBody
			};

			try
			{
				var response = await sqsClient.SendMessageAsync(sendMessageRequest);
				Console.WriteLine($"Message sent to SQS with MessageId: {response.MessageId}");

				// Add response details to the span
				sqsSpan?.SetTag("sqs.message_id", response.MessageId);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to send message to SQS: {ex.Message}");
				sqsSpan?.SetStatus(ActivityStatusCode.Error, ex.Message);
			}
		}
	}

	private static async Task ReceiveMessageFromSqsAsync(string queueUrl)
	{
		// Initialize the SQS client with hardcoded credentials
		var sqsClient = new AmazonSQSClient("YOUR KEY", "YOUR SECRET", Amazon.RegionEndpoint.USEast1);

		var receiveMessageRequest = new ReceiveMessageRequest
		{
			QueueUrl = queueUrl,
			MaxNumberOfMessages = 1,
			WaitTimeSeconds = 10 // Long polling for up to 10 seconds
		};

		try
		{
			var response = await sqsClient.ReceiveMessageAsync(receiveMessageRequest);
			if (response.Messages.Count > 0)
			{
				var message = response.Messages[0];
				Console.WriteLine($"Received message with Body: {message.Body}");

				// Start a new span to show data has been received
				using (var receiveSpan = ActivitySource.StartActivity("MessageReceived"))
				{
					if (receiveSpan != null)
					{
						receiveSpan.SetTag("sqs.message_id", message.MessageId);
						receiveSpan.SetTag("sqs.queue_url", queueUrl);
						receiveSpan.SetTag("sqs.message_body", message.Body);
					}

					// Deserialize the message to extract the context
					var contextData = JsonSerializer.Deserialize<Dictionary<string, string>>(message.Body);

					var traceId = ActivityTraceId.CreateFromString(contextData["TraceId"]);
					var spanId = ActivitySpanId.CreateFromString(contextData["SpanId"]);
					var traceFlags = (ActivityTraceFlags)Enum.Parse(typeof(ActivityTraceFlags), contextData["TraceFlags"]);

					// Create the parent context from the extracted data
					var parentContext = new ActivityContext(traceId, spanId, traceFlags);

					// Start a new activity with the extracted context
					using var receiveActivity = ActivitySource.StartActivity("ReceiveMessageFromSqsAsync", ActivityKind.Consumer, parentContext);

					if (receiveActivity != null)
					{
						// Continue processing the message
						receiveActivity.SetTag("sqs.message_id", message.MessageId);
						receiveActivity.SetTag("sqs.queue_url", queueUrl);
						receiveActivity.SetTag("sqs.message_body", contextData["Message"]);
						Console.WriteLine("Processing message with continued TraceId: " + receiveActivity.Context.TraceId);
					}
				}
			}
			else
			{
				Console.WriteLine("No messages received.");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to receive message from SQS: {ex.Message}");
		}
	}
}
