# 🧠 AskMeNow – AI-Powered Document Intelligence Platform  
*(.NET 8 · WPF · AWS Bedrock Claude 3 Sonnet · EF Core · SQLite)*

---

## 🌟 Overview

**AskMeNow** is an **AI-powered knowledge base** that transforms static documents into **interactive Q&A experiences**.  
It allows us to upload multiple document formats, ask questions in natural language, and receive accurate, contextual answers — all within an intuitive WPF interface.

Whether you're analyzing reports, research papers, or project documentation, AskMeNow makes it effortless to extract insights instantly.

- The problem: Finding information buried inside documents, wikis, and folders is slow and frustrating.
- Who benefits: Individuals and teams who work with PDFs, docs, knowledge bases, or research materials.
- What makes it unique: It combines local document parsing, smart retrieval, and AI chat in one simple WPF app — with a clean architecture.

---

## 🚀 Problem It Solves
Manually reading and searching long documents is slow, inefficient, and often frustrating.  
AskMeNow automates that process by:

- Extracting readable content and metadata from multiple document types.
- Using semantic embeddings and multi-query retrieval for precise understanding.
- Providing **AI-generated answers** using **AWS Bedrock Claude 3 Sonnet**.
- Maintaining context and citations for transparent, trustworthy results.

---

## 💡 Key Features

- ✅ **Smart Q&A** – Ask questions in plain English and get concise, sourced answers.
- ✅ **Document-aware chat** – Handles `.pdf`, `.docx`, `.xlsx`, `.txt`, `.md`, `.json`, `.html`, and `.htm`. Upload files and chat with their contents as if they were a teammate.
- ✅ **Suggested questions** – Get helpful follow‑ups to keep your research moving.
- ✅ **Fast search** – - Fast search: Pulls relevant snippets instead of dumping entire documents at you.
- ✅ **Real-Time Preview** – Instantly preview documents in the side panel.  
- ✅ **Sentiment insights** – Understand tone and intent in conversations when needed.
- ✅ **Speech support** – Speech‑to‑text and text‑to‑speech to talk hands‑free.  
- ✅ **Conversational Memory** – Keeps context across related questions.  
- ✅ **Smart Retrieval Pipeline** – Embeddings, chunking, and multi-query expansion for relevance.  
- ✅ **Export Q&A Sessions** – Save, review, and share your question-answer sessions.  
- ✅ **Clean Architecture + MVVM** – Ensures modularity, scalability, and testability.

---

## 🏗️ Tech Stack

| Layer | Technology |
|-------|-------------|
| **Frontend** | WPF (.NET 8, MVVM) |
| **Backend** | C# (.NET 8), EF Core, SQLite |
| **AI / NLP** | AWS Bedrock Claude 3 Sonnet |
| **Data Handling** | Embeddings, Chunking, Semantic Search |
| **Architecture** | Clean Architecture, SOLID, Repository Pattern |

---

## 🧩 Architecture / Project Structure

The app follows a clean, layered structure so each part has a single, clear purpose.

- UI (`AskMeNow.UI`): The WPF desktop application. Shows views, binds to view models, and handles user interactions.
- Core (`AskMeNow.Core`): The heart of the domain — entities, interfaces, and contracts that define how the system behaves.
- Infrastructure (`AskMeNow.Infrastructure`): The concrete implementations — data access (EF Core/SQLite), external services (AWS Bedrock, Whisper), repositories, and configuration.
- Application (`AskMeNow.Application`): Application‑level coordinators (handlers/services) that orchestrate workflows using Core interfaces.


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


---

## 🪄 Setup & Installation

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- [SQLite](https://www.sqlite.org/download.html)
- AWS Account with Bedrock Access

### Installation Steps

```bash
# 1. Clone the repository
git clone https://github.com/yourusername/AskMeNow.git

# 2. Navigate to project folder
cd AskMeNow

# 3. Restore dependencies
dotnet restore

# 4. Build the project
dotnet build

# 5. Run the WPF application
dotnet run --project src/AskMeNow.UI
```

---

##🧭 Usage Examples

- Launch the app – Start AskMeNow on your desktop.
- Upload Documents – Add your .pdf, .docx, .xlsx, or .txt files.
- Ask Questions – Type questions
- View Answers – AI fetches the relevant context and provides cited responses.
- Preview & Export – Review document in preview panel and export your Q&A history.
---

## Screenshots

### 1. Chat with AI Assistant & Loaded Documents
<img src="https://github.com/user-attachments/assets/8039d909-b3ab-43d0-9387-4d360df40cae" alt="Chat with AI Assistant Loaded Documents" width="900" />

### 2. Document Preview Feature
<img src="https://github.com/user-attachments/assets/6d7ff736-5b96-47fa-9957-26cea8f3f13d" alt="Document Preview Feature" width="900" />

### 3. Show Resource of Answer Feature
<img src="https://github.com/user-attachments/assets/aed9998f-d0c4-45b0-8939-d8e3875e9634" alt="Show Resource of Answer Feature" width="900" />

### 4. Suggested Questions & Chat Export Feature
<img src="https://github.com/user-attachments/assets/37f25e27-a759-4f30-bbec-faf330fac54d" alt="Suggested Questions and Chat Export Feature" width="900" />

---

## Demo

Watch a quick walkthrough: `docs/Videos/Ask Me Now Demo.mp4`

---

## 🔐 Security & Best Practices

- ✅ Secure file handling – prevents path traversal & unsafe uploads.
- ✅ Input validation before ingestion.
- ✅ Async/await used to avoid UI freezing.
- ✅ SOLID principles strictly followed.
- ✅ No hardcoded keys or sensitive data in code.

---

## 🧠 Clean Architecture Principles
- Single Responsibility: Each service handles a distinct concern (e.g., parsing, retrieval, answering).
- Open/Closed: Easily extend new parsers for new file formats.
- Dependency Inversion: Core logic doesn’t depend on infrastructure details.
- Interface Segregation: Separate contracts for parsing, embeddings, and retrieval.
- Liskov Substitution: Services can be interchanged without breaking functionality.

---

## 🧩 Design Patterns Used
| Pattern                    | Purpose                             |
| -------------------------- | ----------------------------------- |
| **Repository Pattern**     | Decouples data access logic         |
| **Factory Pattern**        | Dynamically create file parsers     |
| **Strategy Pattern**       | Switch between retrieval strategies |
| **Command Pattern (MVVM)** | Handle WPF UI commands cleanly      |

---

## Roadmap / Future Enhancements

- Add support for audio files (.mp3, .wav, etc.)
- Add cloud storage integration (Google Drive, OneDrive, S3)
- Add advanced semantic search and embeddings
- Build a web‑based version with the same functionality

---

## 👩‍💻 Contributor
| Name             | Role                             |
| ---------------- | -------------------------------- |
| **Ashita Rathi** | Creator, Developer, and Designer |
