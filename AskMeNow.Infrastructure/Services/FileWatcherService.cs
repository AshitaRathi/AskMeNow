using AskMeNow.Core.Interfaces;
using System.IO;

namespace AskMeNow.Infrastructure.Services;

public class FileWatcherService : IFileWatcherService
{
    private FileSystemWatcher? _fileWatcher;
    private bool _disposed = false;

    public event EventHandler<string>? FileAdded;
    public event EventHandler<string>? FileChanged;
    public event EventHandler<string>? FileDeleted;

    public bool IsWatching => _fileWatcher?.EnableRaisingEvents ?? false;

    public void StartWatching(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

        StopWatching();

        _fileWatcher = new FileSystemWatcher(folderPath)
        {
            Filter = "*.txt",
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        _fileWatcher.Created += OnFileCreated;
        _fileWatcher.Changed += OnFileChanged;
        _fileWatcher.Deleted += OnFileDeleted;
        _fileWatcher.Renamed += OnFileRenamed;
    }

    public void StopWatching()
    {
        if (_fileWatcher != null)
        {
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Created -= OnFileCreated;
            _fileWatcher.Changed -= OnFileChanged;
            _fileWatcher.Deleted -= OnFileDeleted;
            _fileWatcher.Renamed -= OnFileRenamed;
            _fileWatcher.Dispose();
            _fileWatcher = null;
        }
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            FileAdded?.Invoke(this, e.FullPath);
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            FileChanged?.Invoke(this, e.FullPath);
        }
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            FileDeleted?.Invoke(this, e.FullPath);
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        // Treat rename as delete + create
        if (e.OldFullPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            FileDeleted?.Invoke(this, e.OldFullPath);
        }
        
        if (e.FullPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            FileAdded?.Invoke(this, e.FullPath);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopWatching();
            _disposed = true;
        }
    }
}
