namespace AskMeNow.Core.Interfaces;

public interface IFileWatcherService : IDisposable
{
    event EventHandler<string>? FileAdded;
    event EventHandler<string>? FileChanged;
    event EventHandler<string>? FileDeleted;
    
    void StartWatching(string folderPath);
    void StopWatching();
    bool IsWatching { get; }
}
