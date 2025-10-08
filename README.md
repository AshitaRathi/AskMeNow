## Project Title & Tagline

- **AskMeNow**
- A friendly desktop app that lets you chat with your documents in Q/A form. 🎙️🤖

## About

AskMeNow is a Windows desktop app that turns your files and knowledge base into a helpful, conversational assistant. It was created to remove the friction of digging through folders, PDFs, and notes just to find a simple answer. The inspiration came from real-world teams who spend too much time searching instead of doing — AskMeNow brings the answers to you in plain language.

## Overview / Problem Statement

- The problem: Finding information buried inside documents, wikis, and folders is slow and frustrating.
- Who benefits: Individuals and teams who work with PDFs, docs, knowledge bases, or research materials.
- What makes it unique: It combines local document parsing, smart retrieval, and AI chat in one simple WPF app — with a clean architecture.

## Key Features

- Smart Q&A: Ask questions in plain English and get concise, sourced answers.
- Document-aware chat: Upload files and chat with their contents as if they were a teammate.
- Suggested questions: Get helpful follow‑ups to keep your research moving.
- Fast search: Pulls relevant snippets instead of dumping entire documents at you.
- Sentiment insights: Understand tone and intent in conversations when needed.
- Speech support: Optional speech‑to‑text and text‑to‑speech to talk hands‑free.

## Tech Stack

- .NET 8, WPF (Windows)
- EF Core, SQLite (local storage)
- AWS Bedrock Claude (LLM)
- Whisper (speech‑to‑text)

| Area | Technology |
| --- | --- |
| Language & Runtime | .NET 8 (C#) |
| UI | WPF |
| AI / LLM | AWS Bedrock Claude |
| Data Access | Entity Framework Core |
| Database | SQLite (local) |
| Speech | Whisper.NET |

## Architecture / Project Structure

The app follows a clean, layered structure so each part has a single, clear purpose.

- UI (`AskMeNow.UI`): The WPF desktop application. Shows views, binds to view models, and handles user interactions.
- Core (`AskMeNow.Core`): The heart of the domain — entities, interfaces, and contracts that define how the system behaves.
- Infrastructure (`AskMeNow.Infrastructure`): The concrete implementations — data access (EF Core/SQLite), external services (AWS Bedrock, Whisper), repositories, and configuration.
- Application (`AskMeNow.Application`): Application‑level coordinators (handlers/services) that orchestrate workflows using Core interfaces.

Diagram to visualize the layers:

```
┌───────────────────────────┐
│         UI (WPF)          │  -> Views, ViewModels
└─────────────▲─────────────┘
              │ binds/calls
┌─────────────┴─────────────┐
│     Application Layer      │  -> Handlers, Orchestration
└─────────────▲─────────────┘
              │ depends on
┌─────────────┴─────────────┐
│          Core              │  -> Entities, Interfaces
└─────────────▲─────────────┘
              │ implemented by
┌─────────────┴─────────────┐
│       Infrastructure       │  -> EF Core, Repos, Bedrock, STT/TTS
└───────────────────────────┘
```

Project folders of interest:

- `AskMeNow.UI`: Views, view models, and app configuration (`App.xaml`).
- `AskMeNow.Core`: Domain models (e.g., `Conversation`, `DocumentEntity`) and interfaces (e.g., `IEnhancedRetrievalService`).
- `AskMeNow.Infrastructure`: EF Core `KnowledgeBaseContext`, repositories, AWS Bedrock client, speech services, and DI setup.
- `AskMeNow.Application`: High‑level handlers (e.g., `QuestionHandler`) and services that wire the flow together.

## Setup & Installation

Prerequisites:

- Windows 10/11
- .NET 8 SDK
- AWS Bedrock access if you want AI responses
- Microphone for speech features

Step‑by‑step:

1) Clone the repository

```bash
git clone "https://github.com/your-org/AskMeNow.git"
cd "AskMeNow"
```

2) Install dependencies

```bash
dotnet restore
```

3) Configure settings

- Update `AskMeNow.Infrastructure/appsettings.json` and `AskMeNow.UI/appsettings.json` with your region/model if using AWS Bedrock.
- Provide AWS credentials via one of the standard methods:
  - Environment variables: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_REGION`
  - AWS CLI/Shared credentials file
- Database: SQLite is local and works out of the box (no extra setup needed).

4) Run the app

```bash
dotnet run --project "AskMeNow.UI"
```

Alternatively, open the solution `AskMeNow.sln` in Visual Studio, set `AskMeNow.UI` as startup project, then Run.

## Usage Examples

After launching the app:

1) Add your documents (PDFs, text, notes).
2) Type a question in plain English.
3) The app finds the most relevant snippets and answers clearly with sources.
4) Explore suggested follow‑up questions or ask your own.
5) Optionally use speech‑to‑text to dictate questions, and text‑to‑speech to listen to answers.

Example scenario:

- Upload a company FAQ and a few PDFs → Ask “What is our refund policy?” → See a concise answer with a reference and a preview snippet → Click to open the source document if needed.

## Screenshots

Placeholders (replace with your own images in `docs/Screenshots`):

![Home Screen](docs/Screenshots/screenshot-1.png)
![Ask a Question](docs/Screenshots/screenshot-2.png)
![Search Results](docs/Screenshots/screenshot-3.png)
![Document Preview](docs/Screenshots/screenshot-4.png)

## Demo

Watch a quick walkthrough:

- Local video: `docs/Videos/Ask Me Now Demo.mp4`
- Placeholder path: `/docs/videos/demo.mp4`
- Or add an online link: `https://your-demo-link.example.com`

## Roadmap / Future Enhancements

- Add support for audio files (.mp3, .wav, etc.)
- Add cloud storage integration (Google Drive, OneDrive, S3)
- Add advanced semantic search and embeddings
- Build a web‑based version with the same functionality

## Security & Best Practices

We care about safety, reliability, and maintainability:

- Safe file handling: Prevents path traversal and restricts file access to allowed locations.
- Input validation: Cleans and validates content before ingestion and processing.
- Async operations: Uses asynchronous calls to keep the UI responsive and fast.
- SOLID principles & Clean Architecture: Clear boundaries between UI, Core, Infrastructure, and Application.
- Repository pattern: Predictable and testable data access.

---

Made with ❤️ to help you spend less time searching and more time doing. If you like it, a ⭐ on GitHub goes a long way!


