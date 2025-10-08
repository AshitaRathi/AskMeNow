using AskMeNow.Application.Handlers;
using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AskMeNow.UI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IQuestionHandler _questionHandler;
        private readonly ISpeechToTextService _speechToTextService;
        private readonly ITextToSpeechService _textToSpeechService;
        private readonly IPersistentKnowledgeBaseService _knowledgeBaseService;
        private readonly IFileWatcherService _fileWatcherService;
        private readonly IConversationService _conversationService;
        private readonly IAutoSuggestService _autoSuggestService;
        private readonly IDocumentPreviewService _documentPreviewService;
        private readonly IEnhancedRetrievalService _enhancedRetrievalService;
        private string _userQuestion = string.Empty;
        private bool _isLoading = false;
        private string _statusMessage = "Welcome to AskMeNow! Please select a folder containing documents to begin.";
        private bool _isDocumentsLoaded = false;
        private bool _isFolderSelectionInProgress = false;
        private bool _isRecording = false;
        private bool _isVoiceEnabled = true;
        private bool _isMuted = false;
        private bool _showSources = false;
        private float _recordingLevel = 0f;
        private CancellationTokenSource? _recordingCancellationTokenSource;
        private ObservableCollection<Message> _messages = new();
        private ObservableCollection<DocumentInfo> _loadedDocuments = new();
        private string _currentConversationId = string.Empty;
        private bool _enableConversationContext = true;
        private bool _enableAutoSuggestions = true;

        public MainViewModel(
            IQuestionHandler questionHandler,
            ISpeechToTextService speechToTextService,
            ITextToSpeechService textToSpeechService,
            IPersistentKnowledgeBaseService knowledgeBaseService,
            IFileWatcherService fileWatcherService,
            IConversationService conversationService,
            IAutoSuggestService autoSuggestService,
            IDocumentPreviewService documentPreviewService,
            IEnhancedRetrievalService enhancedRetrievalService)
        {
            _questionHandler = questionHandler;
            _speechToTextService = speechToTextService;
            _textToSpeechService = textToSpeechService;
            _knowledgeBaseService = knowledgeBaseService;
            _fileWatcherService = fileWatcherService;
            _conversationService = conversationService;
            _autoSuggestService = autoSuggestService;
            _documentPreviewService = documentPreviewService;
            _enhancedRetrievalService = enhancedRetrievalService;

            AskCommand = new RelayCommand(async () => await AskQuestionAsync(), () => !IsLoading && IsDocumentsLoaded && !string.IsNullOrWhiteSpace(UserQuestion));
            SelectFolderCommand = new RelayCommand(SelectFolder);
            ExportCommand = new RelayCommand(ExportConversation, () => Messages.Any(m => m.Sender == MessageSender.User));
            EnterKeyCommand = new RelayCommand(async () => await HandleEnterKeyAsync());
            VoiceAskCommand = new RelayCommand(async () => await StartVoiceRecordingAsync(), () => !IsLoading && IsDocumentsLoaded && IsVoiceEnabled && !IsRecording);
            StopRecordingCommand = new RelayCommand(StopVoiceRecording, () => IsRecording);
            ToggleMuteCommand = new RelayCommand(ToggleMute);
            ToggleShowSourcesCommand = new RelayCommand(ToggleShowSources);
            NewConversationCommand = new RelayCommand(async () => await StartNewConversationAsync());
            ToggleConversationContextCommand = new RelayCommand(ToggleConversationContext);
            ToggleAutoSuggestionsCommand = new RelayCommand(ToggleAutoSuggestions);

            _speechToTextService.RecordingStarted += OnRecordingStarted;
            _speechToTextService.RecordingStopped += OnRecordingStopped;
            _speechToTextService.RecordingLevelChanged += OnRecordingLevelChanged;
            _textToSpeechService.SpeechStarted += OnSpeechStarted;
            _textToSpeechService.SpeechCompleted += OnSpeechCompleted;
            _textToSpeechService.SpeechError += OnSpeechError;

            _fileWatcherService.FileAdded += OnFileAdded;
            _fileWatcherService.FileChanged += OnFileChanged;
            _fileWatcherService.FileDeleted += OnFileDeleted;

            AddMessage("Welcome to AskMeNow! Loading your documents...", MessageSender.AI);

            _ = Task.Run(async () => await InitializeServicesAsync());

            _ = Task.Run(async () => await StartNewConversationAsync());
        }

        public string UserQuestion
        {
            get => _userQuestion;
            set
            {
                _userQuestion = value;
                OnPropertyChanged();
                ((RelayCommand)AskCommand).RaiseCanExecuteChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                ((RelayCommand)AskCommand).RaiseCanExecuteChanged();
                ((RelayCommand)VoiceAskCommand).RaiseCanExecuteChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsDocumentsLoaded
        {
            get => _isDocumentsLoaded;
            set
            {
                _isDocumentsLoaded = value;
                OnPropertyChanged();
                ((RelayCommand)AskCommand).RaiseCanExecuteChanged();
                ((RelayCommand)VoiceAskCommand).RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<Message> Messages
        {
            get => _messages;
            set
            {
                _messages = value;
                OnPropertyChanged();
                ((RelayCommand)ExportCommand).RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<DocumentInfo> LoadedDocuments
        {
            get => _loadedDocuments;
            set
            {
                _loadedDocuments = value;
                OnPropertyChanged();
            }
        }

        public ICommand AskCommand { get; }
        public ICommand SelectFolderCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand EnterKeyCommand { get; }
        public ICommand VoiceAskCommand { get; }
        public ICommand StopRecordingCommand { get; }
        public ICommand ToggleMuteCommand { get; }
        public ICommand ToggleShowSourcesCommand { get; }
        public ICommand NewConversationCommand { get; }
        public ICommand ToggleConversationContextCommand { get; }
        public ICommand ToggleAutoSuggestionsCommand { get; }

        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                _isRecording = value;
                OnPropertyChanged();
                ((RelayCommand)VoiceAskCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopRecordingCommand).RaiseCanExecuteChanged();
            }
        }

        public bool IsVoiceEnabled
        {
            get => _isVoiceEnabled;
            set
            {
                _isVoiceEnabled = value;
                OnPropertyChanged();
                ((RelayCommand)VoiceAskCommand).RaiseCanExecuteChanged();
            }
        }

        public float RecordingLevel
        {
            get => _recordingLevel;
            set
            {
                _recordingLevel = value;
                OnPropertyChanged();
            }
        }

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                _isMuted = value;
                OnPropertyChanged();
            }
        }

        public bool ShowSources
        {
            get => _showSources;
            set
            {
                _showSources = value;
                OnPropertyChanged();
            }
        }

        public string CurrentConversationId
        {
            get => _currentConversationId;
            set
            {
                _currentConversationId = value;
                OnPropertyChanged();
            }
        }

        public bool EnableConversationContext
        {
            get => _enableConversationContext;
            set
            {
                _enableConversationContext = value;
                OnPropertyChanged();
            }
        }

        public bool EnableAutoSuggestions
        {
            get => _enableAutoSuggestions;
            set
            {
                _enableAutoSuggestions = value;
                OnPropertyChanged();
            }
        }

        public IDocumentPreviewService DocumentPreviewService => _documentPreviewService;

        private async Task AskQuestionAsync()
        {
            if (string.IsNullOrWhiteSpace(UserQuestion))
                return;

            var question = UserQuestion.Trim();
            UserQuestion = string.Empty;

            AddMessage(question, MessageSender.User);

            var loadingMessage = new Message
            {
                Text = "Thinking...",
                Sender = MessageSender.AI,
                IsLoading = true
            };
            Messages.Add(loadingMessage);

            IsLoading = true;
            StatusMessage = "Processing your question...";

            try
            {
                var conversationId = EnableConversationContext && !string.IsNullOrEmpty(CurrentConversationId)
                    ? CurrentConversationId
                    : string.Empty;

                var answer = await _questionHandler.ProcessQuestionAsync(question, conversationId);

                Messages.Remove(loadingMessage);

                AddMessage(answer.Answer, MessageSender.AI, answer.DocumentSnippets, answer.SuggestedQuestions);

                if (answer.DocumentSnippets?.Any() == true)
                {
                    _ = Task.Run(async () => await UpdateDocumentHighlightsAsync(answer.DocumentSnippets));
                }

                if (IsVoiceEnabled)
                {
                    _ = Task.Run(async () => await SpeakResponseAsync(answer.Answer));
                }

                StatusMessage = "Ready to answer your questions.";
            }
            catch (Exception ex)
            {
                Messages.Remove(loadingMessage);

                AddMessage($"Sorry, I encountered an error: {ex.Message}", MessageSender.AI);
                StatusMessage = "An error occurred while processing your question.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task HandleEnterKeyAsync()
        {
            // Check if Shift is pressed for new lines
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Shift)
            {
                // Allow new line, don't send
                return;
            }

            // Send the message
            await AskQuestionAsync();
        }

        private void AddMessage(string text, MessageSender sender, List<DocumentSnippet>? documentSnippets = null, List<SuggestedQuestion>? suggestedQuestions = null)
        {
            var message = new Message
            {
                Text = text,
                Sender = sender,
                Timestamp = DateTime.Now,
                DocumentSnippets = documentSnippets ?? new List<DocumentSnippet>(),
                SuggestedQuestions = suggestedQuestions ?? new List<SuggestedQuestion>()
            };

            Messages.Add(message);

            ((RelayCommand)ExportCommand).RaiseCanExecuteChanged();
        }

        private async void SelectFolder()
        {
            if (_isFolderSelectionInProgress)
                return;

            _isFolderSelectionInProgress = true;

            try
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "Select folder containing documents"
                };

                if (dialog.ShowDialog() == true)
                {
                    await LoadDocumentsAsync(dialog.FolderName);

                    _fileWatcherService.StartWatching(dialog.FolderName);
                }
            }
            finally
            {
                _isFolderSelectionInProgress = false;
            }
        }

        public async Task LoadDocumentsAsync(string folderPath)
        {
            IsLoading = true;
            StatusMessage = "Loading documents...";

            var loadingMessage = new Message
            {
                Text = "Loading documents from selected folder...",
                Sender = MessageSender.AI,
                IsLoading = true
            };
            Messages.Add(loadingMessage);

            try
            {
                var documents = await _questionHandler.InitializeDocumentsAsync(folderPath);

                Messages.Remove(loadingMessage);

                LoadedDocuments.Clear();
                foreach (var doc in documents)
                {
                    var fileInfo = new FileInfo(doc.FilePath);
                    var documentInfo = new DocumentInfo
                    {
                        Title = doc.Title,
                        FilePath = doc.FilePath,
                        FileExtension = Path.GetExtension(doc.FilePath).ToLowerInvariant(),
                        WordCount = CountWords(doc.Content),
                        FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
                        LastModified = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.Now,
                        Content = doc.Content
                    };
                    LoadedDocuments.Add(documentInfo);
                }

                IsDocumentsLoaded = true;

                await ValidateEmbeddingsAsync();

                var processingResult = _questionHandler.GetLastProcessingResult();

                if (processingResult != null)
                {
                    var supportedExtensions = string.Join(", ", processingResult.SupportedExtensions.Select(ext => ext.TrimStart('.')));
                    var unsupportedExtensions = processingResult.UnsupportedExtensions.Any()
                        ? string.Join(", ", processingResult.UnsupportedExtensions.Select(ext => ext.TrimStart('.')))
                        : "none";

                    StatusMessage = $"Loaded {processingResult.SuccessfullyProcessed} supported files. Skipped {processingResult.UnsupportedFilesFound} unsupported files.";

                    var message = $"✅ Document processing complete!\n\n";
                    message += $"📄 **Successfully processed:** {processingResult.SuccessfullyProcessed} files\n";
                    message += $"📁 **Supported file types:** {supportedExtensions}\n";

                    if (processingResult.UnsupportedFilesFound > 0)
                    {
                        message += $"⚠️ **Skipped unsupported files:** {processingResult.UnsupportedFilesFound} files\n";
                        message += $"🚫 **Unsupported file types:** {unsupportedExtensions}\n";
                    }

                    if (processingResult.FailedToProcess > 0)
                    {
                        message += $"❌ **Failed to process:** {processingResult.FailedToProcess} files\n";
                    }

                    message += $"\nYou can now ask questions about the content from any of the processed documents!";

                    AddMessage(message, MessageSender.AI);
                }
                else
                {
                    var fileTypeCounts = documents
                        .GroupBy(d => Path.GetExtension(d.FilePath).ToLowerInvariant())
                        .ToDictionary(g => g.Key, g => g.Count());

                    var fileTypeSummary = string.Join(", ", fileTypeCounts.Select(kvp => $"{kvp.Value} {kvp.Key.TrimStart('.')}"));
                    StatusMessage = $"Loaded {documents.Count} files ({fileTypeSummary}). Ready to answer questions!";

                    var fileTypeDetails = string.Join(", ", fileTypeCounts.Select(kvp => $"{kvp.Value} {kvp.Key.TrimStart('.')} files"));
                    AddMessage($"✅ Successfully loaded {documents.Count} documents from the selected folder:\n\n📄 {fileTypeDetails}\n\nYou can now ask questions about the content from any of these documents!", MessageSender.AI);
                }
            }
            catch (Exception ex)
            {
                Messages.Remove(loadingMessage);

                StatusMessage = $"Error loading documents: {ex.Message}";
                IsDocumentsLoaded = false;

                AddMessage($"❌ Failed to load documents: {ex.Message}", MessageSender.AI);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExportConversation()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export conversation",
                Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                DefaultExt = "md"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var content = "# AskMeNow Conversation Export\n\n";
                    content += $"**Exported on:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n";

                    var userMessages = Messages.Where(m => m.Sender == MessageSender.User).ToList();
                    var aiMessages = Messages.Where(m => m.Sender == MessageSender.AI && !m.IsLoading).ToList();

                    for (int i = 0; i < userMessages.Count; i++)
                    {
                        content += $"## Q: {userMessages[i].Text}\n\n";
                        content += $"**Asked:** {userMessages[i].Timestamp:yyyy-MM-dd HH:mm:ss}\n\n";

                        if (i < aiMessages.Count)
                        {
                            content += $"**A:** {aiMessages[i].Text}\n\n";
                            content += $"**Answered:** {aiMessages[i].Timestamp:yyyy-MM-dd HH:mm:ss}\n\n";
                        }

                        content += "---\n\n";
                    }

                    File.WriteAllText(dialog.FileName, content);
                    StatusMessage = $"Conversation exported to {Path.GetFileName(dialog.FileName)}";
                    AddMessage($"📄 Conversation exported successfully to {Path.GetFileName(dialog.FileName)}", MessageSender.AI);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error exporting conversation: {ex.Message}";
                    AddMessage($"❌ Error exporting conversation: {ex.Message}", MessageSender.AI);
                }
            }
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        #region Voice Interaction Methods

        private async Task InitializeServicesAsync()
        {
            try
            {
                await _knowledgeBaseService.InitializeAsync();

                await _speechToTextService.InitializeAsync();
            }
            catch (Exception ex)
            {
                IsVoiceEnabled = false;
                System.Diagnostics.Debug.WriteLine($"Services initialization failed: {ex.Message}");
            }
        }

        private async Task StartVoiceRecordingAsync()
        {
            if (IsRecording) return;

            try
            {
                IsRecording = true;
                StatusMessage = "Recording... Speak your question";

                _recordingCancellationTokenSource = new CancellationTokenSource();

                var transcribedText = await _speechToTextService.RecordAndTranscribeAsync(15, _recordingCancellationTokenSource.Token);

                if (!string.IsNullOrWhiteSpace(transcribedText) && transcribedText != "No speech detected")
                {
                    UserQuestion = transcribedText;

                    await AskQuestionAsync();
                }
                else
                {
                    StatusMessage = "No speech detected. Please try again.";
                    AddMessage("No speech was detected. Please try speaking more clearly or check your microphone.", MessageSender.AI);
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Recording cancelled";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Voice recording error: {ex.Message}";
                AddMessage($"❌ Voice recording failed: {ex.Message}", MessageSender.AI);
            }
            finally
            {
                IsRecording = false;
                _recordingCancellationTokenSource?.Dispose();
                _recordingCancellationTokenSource = null;
            }
        }

        private void StopVoiceRecording()
        {
            _recordingCancellationTokenSource?.Cancel();
        }

        private void ToggleMute()
        {
            IsMuted = !IsMuted;

            if (IsMuted && _textToSpeechService.IsSpeaking)
            {
                _textToSpeechService.StopSpeaking();
            }
        }

        private void ToggleShowSources()
        {
            ShowSources = !ShowSources;
        }

        #region File Watcher Event Handlers

        private async void OnFileAdded(object? sender, string filePath)
        {
            try
            {
                await _knowledgeBaseService.ProcessDocumentAsync(filePath);
                System.Diagnostics.Debug.WriteLine($"Processed new file: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing new file {filePath}: {ex.Message}");
            }
        }

        private async void OnFileChanged(object? sender, string filePath)
        {
            try
            {
                await _knowledgeBaseService.UpdateDocumentAsync(filePath);
                System.Diagnostics.Debug.WriteLine($"Updated file: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating file {filePath}: {ex.Message}");
            }
        }

        private async void OnFileDeleted(object? sender, string filePath)
        {
            try
            {
                await _knowledgeBaseService.DeleteDocumentAsync(filePath);
                System.Diagnostics.Debug.WriteLine($"Deleted file: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting file {filePath}: {ex.Message}");
            }
        }

        #endregion

        #region Conversation Management Methods

        private async Task StartNewConversationAsync()
        {
            try
            {
                CurrentConversationId = await _conversationService.CreateNewConversationAsync();

                if (Messages.Count > 1)
                {
                    Messages.Clear();
                    AddMessage("New conversation started! How can I help you today?", MessageSender.AI);
                }
                else
                {
                    if (Messages.Count == 1 && Messages[0].Text.Contains("Welcome to AskMeNow"))
                    {
                        Messages[0].Text = "Welcome to AskMeNow! How can I help you today?";
                    }
                }

                StatusMessage = "New conversation started.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to start new conversation: {ex.Message}";
            }
        }

        private void ToggleConversationContext()
        {
            EnableConversationContext = !EnableConversationContext;
            StatusMessage = EnableConversationContext
                ? "Conversation context enabled - AI will remember previous exchanges"
                : "Conversation context disabled - each question is treated independently";
        }

        private void ToggleAutoSuggestions()
        {
            EnableAutoSuggestions = !EnableAutoSuggestions;
            StatusMessage = EnableAutoSuggestions
                ? "Auto-suggestions enabled - follow-up questions will be suggested"
                : "Auto-suggestions disabled";
        }

        public async Task HandleSuggestedQuestionClickAsync(string suggestedQuestion)
        {
            if (string.IsNullOrWhiteSpace(suggestedQuestion))
                return;

            UserQuestion = suggestedQuestion;
            await AskQuestionAsync();
        }

        #endregion

        private async Task SpeakResponseAsync(string text)
        {
            if (!IsVoiceEnabled || IsMuted || string.IsNullOrWhiteSpace(text)) return;

            try
            {
                await _textToSpeechService.SpeakAsync(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TTS Error: {ex.Message}");
            }
        }

        #endregion

        #region Voice Service Event Handlers

        private void OnRecordingStarted(object? sender, EventArgs e)
        {
        }

        private void OnRecordingStopped(object? sender, EventArgs e)
        {
        }

        private void OnRecordingLevelChanged(object? sender, float level)
        {
            RecordingLevel = level;
        }

        private void OnSpeechStarted(object? sender, EventArgs e)
        {
        }

        private void OnSpeechCompleted(object? sender, EventArgs e)
        {
        }

        private void OnSpeechError(object? sender, string error)
        {
            System.Diagnostics.Debug.WriteLine($"TTS Error: {error}");
        }

        #endregion

        #region Document Preview Methods

        private async Task UpdateDocumentHighlightsAsync(List<DocumentSnippet> documentSnippets)
        {
            try
            {
                var snippetsByFile = documentSnippets.GroupBy(s => s.FilePath);

                foreach (var group in snippetsByFile)
                {
                    var filePath = group.Key;
                    var snippets = group.ToList();

                    await _documentPreviewService.UpdateHighlightsAsync(filePath, snippets);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating document highlights: {ex.Message}");
            }
        }

        #endregion

        #region Validation Methods

        private async Task ValidateEmbeddingsAsync()
        {
            try
            {
                StatusMessage = "Validating embeddings...";

                var validationResult = await _enhancedRetrievalService.ValidateEmbeddingsAsync();

                if (validationResult.IsValid)
                {
                    StatusMessage = $"Embeddings validated successfully. Average similarity: {validationResult.AverageSimilarity:F2}";
                    System.Diagnostics.Debug.WriteLine($"Embedding validation successful. Tested {validationResult.TestedEmbeddings} embeddings with average similarity {validationResult.AverageSimilarity:F3}");
                }
                else
                {
                    StatusMessage = "Embedding validation completed with warnings. Check debug output for details.";
                    System.Diagnostics.Debug.WriteLine($"Embedding validation failed. Errors: {string.Join(", ", validationResult.Errors)}");

                    foreach (var error in validationResult.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Validation Error: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Embedding validation failed. Check debug output for details.";
                System.Diagnostics.Debug.WriteLine($"Error during embedding validation: {ex.Message}");
            }
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _executeAsync;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            _executeAsync = executeAsync;
            _canExecute = canExecute ?? (() => true);
        }

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _executeAsync = () => { execute(); return Task.CompletedTask; };
            _canExecute = canExecute ?? (() => true);
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute();

        public async void Execute(object? parameter)
        {
            await _executeAsync();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}