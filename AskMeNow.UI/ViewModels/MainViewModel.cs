using AskMeNow.Application.Handlers;
using AskMeNow.Core.Entities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AskMeNow.UI.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IQuestionHandler _questionHandler;
    private string _userQuestion = string.Empty;
    private bool _isLoading = false;
    private string _statusMessage = "Welcome to AskMeNow! Please select a folder containing documents to begin.";
    private bool _isDocumentsLoaded = false;
    private bool _isFolderSelectionInProgress = false;
    private ObservableCollection<Message> _messages = new();
    private ObservableCollection<DocumentInfo> _loadedDocuments = new();

    public MainViewModel(IQuestionHandler questionHandler)
    {
        _questionHandler = questionHandler;
        AskCommand = new RelayCommand(async () => await AskQuestionAsync(), () => !IsLoading && IsDocumentsLoaded && !string.IsNullOrWhiteSpace(UserQuestion));
        SelectFolderCommand = new RelayCommand(SelectFolder);
        ExportCommand = new RelayCommand(ExportConversation, () => Messages.Any(m => m.Sender == MessageSender.User));
        EnterKeyCommand = new RelayCommand(async () => await HandleEnterKeyAsync());
        
        // Add welcome message
        AddMessage("Welcome to AskMeNow! Loading your documents...", MessageSender.AI);
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

    private async Task AskQuestionAsync()
    {
        if (string.IsNullOrWhiteSpace(UserQuestion))
            return;

        var question = UserQuestion.Trim();
        UserQuestion = string.Empty;

        // Add user message to chat
        AddMessage(question, MessageSender.User);

        // Add loading message
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
            var answer = await _questionHandler.ProcessQuestionAsync(question);
            
            // Remove loading message
            Messages.Remove(loadingMessage);
            
            // Add AI response
            AddMessage(answer.Answer, MessageSender.AI);
            
            StatusMessage = "Ready to answer your questions.";
        }
        catch (Exception ex)
        {
            // Remove loading message
            Messages.Remove(loadingMessage);
            
            // Add error message
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

    private void AddMessage(string text, MessageSender sender)
    {
        var message = new Message
        {
            Text = text,
            Sender = sender,
            Timestamp = DateTime.Now
        };
        
        Messages.Add(message);
        
        // Trigger export command update
        ((RelayCommand)ExportCommand).RaiseCanExecuteChanged();
    }

    private async void SelectFolder()
    {
        // Prevent multiple folder selection dialogs
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
        
        // Add loading message
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
            
            // Remove loading message
            Messages.Remove(loadingMessage);
            
            // Populate the loaded documents collection for the sidebar
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
            
            // Get file processing statistics
            var processingResult = _questionHandler.GetLastProcessingResult();
            
            if (processingResult != null)
            {
                // Create detailed status message
                var supportedExtensions = string.Join(", ", processingResult.SupportedExtensions.Select(ext => ext.TrimStart('.')));
                var unsupportedExtensions = processingResult.UnsupportedExtensions.Any() 
                    ? string.Join(", ", processingResult.UnsupportedExtensions.Select(ext => ext.TrimStart('.')))
                    : "none";
                
                StatusMessage = $"Loaded {processingResult.SuccessfullyProcessed} supported files. Skipped {processingResult.UnsupportedFilesFound} unsupported files.";
                
                // Add detailed success message
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
                // Fallback to simple counting if processing result is not available
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
            // Remove loading message
            Messages.Remove(loadingMessage);
            
            StatusMessage = $"Error loading documents: {ex.Message}";
            IsDocumentsLoaded = false;
            
            // Add error message
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