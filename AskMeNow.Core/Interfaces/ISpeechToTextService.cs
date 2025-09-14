namespace AskMeNow.Core.Interfaces;

public interface ISpeechToTextService
{
    /// <summary>
    /// Initializes the speech-to-text service
    /// </summary>
    /// <returns>Task representing the initialization operation</returns>
    Task InitializeAsync();
    
    /// <summary>
    /// Records audio from the microphone and transcribes it to text using Whisper.NET
    /// </summary>
    /// <param name="maxRecordingDurationSeconds">Maximum recording duration in seconds (default: 15)</param>
    /// <param name="cancellationToken">Cancellation token to stop recording</param>
    /// <returns>The transcribed text from the audio</returns>
    Task<string> RecordAndTranscribeAsync(int maxRecordingDurationSeconds = 15, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Transcribes an existing audio file to text
    /// </summary>
    /// <param name="audioFilePath">Path to the audio file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transcribed text from the audio file</returns>
    Task<string> TranscribeAudioFileAsync(string audioFilePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if the service is properly initialized and ready to use
    /// </summary>
    bool IsInitialized { get; }
    
    /// <summary>
    /// Event fired when recording starts
    /// </summary>
    event EventHandler? RecordingStarted;
    
    /// <summary>
    /// Event fired when recording stops
    /// </summary>
    event EventHandler? RecordingStopped;
    
    /// <summary>
    /// Event fired during recording with current recording level (0.0 to 1.0)
    /// </summary>
    event EventHandler<float>? RecordingLevelChanged;
}
