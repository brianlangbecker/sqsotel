# MyConsoleApp

This project demonstrates how to use OpenTelemetry with .NET to send and receive messages from Amazon SQS while tracing the context through activities and spans. The application uses the OTLP exporter to send telemetry data to Honeycomb.io.

## Prerequisites

- .NET SDK
- AWS credentials with access to Amazon SQS
- Honeycomb.io account and API key

## Installation

1. **Clone the repository:**

   ```bash
   git clone https://github.com/your-repository/my-console-app.git
   cd my-console-app
   ```

2. **Install dependencies:**

   - Ensure you have the necessary NuGet packages:
     ```bash
     dotnet add package OpenTelemetry --version x.y.z
     dotnet add package OpenTelemetry.Exporter.Console --version x.y.z
     dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol --version x.y.z
     dotnet add package AWSSDK.SQS --version x.y.z
     ```

3. **Configure environment variables:**
   - Replace `"YOUR API KEY"` with your Honeycomb API key.
   - Replace `"YOUR SQS QUEUE URL"` with your SQS Queue URL.
   - Replace `"YOUR KEY"` and `"YOUR SECRET"` with your AWS credentials.

## Usage

1. **Build and run the application:**

   ```bash
   dotnet run
   ```

2. **Expected Output:**
   - The console should display the creation of an activity with a `TraceId`.
   - The application will send a message to the specified SQS queue, including the serialized activity context in the message body.
   - It will then receive the message and continue the trace, showing that the `TraceId` is maintained throughout.

## Project Structure

- **`Program.cs`**: The main entry point of the application, where the OpenTelemetry SDK is configured and the logic for sending and receiving messages from SQS is implemented.

## Key Concepts

- **OpenTelemetry**: A set of APIs and instrumentation libraries that allow developers to collect distributed traces and metrics.
- **ActivitySource & Activity**: The .NET classes used to create and track operations (spans) within your application.
- **OTLP Exporter**: An OpenTelemetry exporter that sends trace data to Honeycomb.io.
- **Amazon SQS**: A fully managed message queuing service from AWS that allows you to decouple and scale microservices, distributed systems, and serverless applications.

## Important Notes

- **Security**: Do not hardcode your AWS credentials and Honeycomb API key in your code for production use. Use secure methods like environment variables or AWS IAM roles.

- **Console Exporter**: The console exporter is included to visualize the trace output in the terminal. This can be removed or replaced in production environments.

## Troubleshooting

- **No messages received:** If you encounter an issue where no messages are received, ensure that your SQS queue URL and AWS credentials are correct and that the SQS queue has messages.

- **Activity not created:** If the activity isn't created, ensure that your OpenTelemetry configuration is correct, and the necessary NuGet packages are installed.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
