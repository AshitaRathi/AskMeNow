# AskMeNow - AI-Powered FAQ Bot

A local FAQ Bot that uses AWS Bedrock models to answer user questions based on given documents/text context. Built with .NET 8, WPF, and Clean Architecture principles.

## Features

- 🤖 **AI-Powered Answers**: Uses AWS Bedrock (Claude 3 Sonnet) for intelligent question answering
- 📁 **Document Loading**: Select a folder containing `.txt` files to build your knowledge base
- 🎨 **Modern UI**: Clean, modern WPF interface with rounded corners and soft shadows
- 💾 **Export Conversations**: Export your Q&A sessions to Markdown files
- 🏗️ **Clean Architecture**: Follows Clean Architecture principles with proper separation of concerns
- ⚡ **Real-time Processing**: Fast, responsive question answering

## Prerequisites

- .NET 8 SDK
- AWS Account with Bedrock access
- Windows OS (for WPF)

## Setup Instructions

### 1. Clone and Build

```bash
git clone <repository-url>
cd AskMeNow
dotnet restore
dotnet build
```

### 2. Configure AWS Credentials

1. Open `AskMeNow.UI/appsettings.json`
2. Replace the placeholder values with your actual AWS credentials:

```json
{
  "AwsConfig": {
    "AccessKey": "YOUR_ACTUAL_AWS_ACCESS_KEY",
    "SecretKey": "YOUR_ACTUAL_AWS_SECRET_KEY",
    "Region": "us-east-1",
    "ModelId": "anthropic.claude-3-sonnet-20240229-v1:0"
  }
}
```

**Important Security Note**: Never commit your actual AWS credentials to version control. Consider using AWS IAM roles or environment variables for production use.

### 3. AWS Bedrock Setup

1. **Enable Bedrock Access**:
   - Go to AWS Console → Amazon Bedrock
   - Request access to the Claude 3 Sonnet model
   - Wait for approval (usually takes a few minutes)

2. **IAM Permissions**:
   Ensure your AWS user/role has the following permissions:
   ```json
   {
     "Version": "2012-10-17",
     "Statement": [
       {
         "Effect": "Allow",
         "Action": [
           "bedrock:InvokeModel"
         ],
         "Resource": "arn:aws:bedrock:*::foundation-model/anthropic.claude-3-sonnet-20240229-v1:0"
       }
     ]
   }
   ```

### 4. Prepare Your Documents

1. Create a folder containing your `.txt` files
2. Each `.txt` file will be treated as a separate document
3. The content of all files will be combined to create your knowledge base

**Example folder structure**:
```
MyDocuments/
├── company-policies.txt
├── product-manual.txt
├── faq-answers.txt
└── troubleshooting-guide.txt
```

### 5. Run the Application

```bash
dotnet run --project AskMeNow.UI
```

## Usage

1. **Launch the Application**: Run the WPF application
2. **Select Documents Folder**: Click "Select Documents Folder" and choose the folder containing your `.txt` files
3. **Wait for Loading**: The app will load and process all documents
4. **Ask Questions**: Type your questions in the text box and click "Ask Question"
5. **View Answers**: AI-generated answers will appear in the response area
6. **Export Conversations**: Click "Export Conversation" to save your Q&A session as a Markdown file

## Project Structure

```
AskMeNow/
├── AskMeNow.Core/                 # Domain entities
│   └── Entities/
│       ├── FAQDocument.cs
│       └── FAQAnswer.cs
├── AskMeNow.Application/          # Use cases and business logic
│   ├── Services/
│   │   └── FAQService.cs
│   └── Handlers/
│       └── QuestionHandler.cs
├── AskMeNow.Infrastructure/       # External services and data access
│   ├── Services/
│   │   └── BedrockClientService.cs
│   ├── Repositories/
│   │   └── DocumentRepository.cs
│   └── Configuration/
│       └── AwsConfig.cs
├── AskMeNow.UI/                   # WPF user interface
│   ├── Views/
│   │   ├── MainWindow.xaml
│   │   └── MainWindow.xaml.cs
│   └── ViewModels/
│       └── MainViewModel.cs
└── README.md
```

## Architecture

The solution follows Clean Architecture principles:

- **Core**: Contains domain entities and business rules
- **Application**: Contains use cases and application logic
- **Infrastructure**: Contains external services (AWS Bedrock, file system)
- **UI**: Contains the WPF user interface with MVVM pattern

## Configuration Options

### AWS Bedrock Models

You can change the model by modifying the `ModelId` in `appsettings.json`:

- `anthropic.claude-3-sonnet-20240229-v1:0` (Default - Recommended)
- `anthropic.claude-3-haiku-20240307-v1:0` (Faster, less capable)
- `amazon.titan-text-express-v1` (Alternative option)

### AWS Regions

Supported regions include:
- `us-east-1` (N. Virginia) - Default
- `us-west-2` (Oregon)
- `eu-west-1` (Ireland)
- `ap-southeast-1` (Singapore)

## Troubleshooting

### Common Issues

1. **"Access Denied" Error**:
   - Verify your AWS credentials are correct
   - Ensure you have Bedrock access enabled
   - Check IAM permissions

2. **"Model Not Available" Error**:
   - Request access to the Claude model in AWS Bedrock console
   - Wait for approval (can take a few minutes)

3. **"No Documents Loaded" Error**:
   - Ensure the selected folder contains `.txt` files
   - Check file permissions
   - Verify files are not empty

4. **Slow Response Times**:
   - Large documents may take longer to process
   - Consider breaking large files into smaller chunks
   - Check your internet connection

### Performance Tips

- Keep individual `.txt` files under 1MB for optimal performance
- Use descriptive filenames to help with context
- Organize related content in the same file
- Avoid duplicate content across files

## Development

### Building from Source

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests (if any)
dotnet test

# Run the application
dotnet run --project AskMeNow.UI
```

### Adding New Features

1. **New Domain Entities**: Add to `AskMeNow.Core/Entities/`
2. **New Use Cases**: Add to `AskMeNow.Application/Services/`
3. **New External Services**: Add to `AskMeNow.Infrastructure/Services/`
4. **New UI Components**: Add to `AskMeNow.UI/Views/` and `ViewModels/`

## License

This project is licensed under the MIT License.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## Support

For issues and questions:
1. Check the troubleshooting section above
2. Review AWS Bedrock documentation
3. Open an issue in the repository

---

**Note**: This application requires an active internet connection to communicate with AWS Bedrock services. AWS charges apply for Bedrock API usage.
