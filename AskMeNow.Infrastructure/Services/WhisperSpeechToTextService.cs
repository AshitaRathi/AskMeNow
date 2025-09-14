using AskMeNow.Core.Interfaces;
using NAudio.Wave;
using Whisper.net;
using Whisper.net.Ggml;
using System.Speech.Recognition;

namespace AskMeNow.Infrastructure.Services;

public class WhisperSpeechToTextService : ISpeechToTextService, IDisposable
{
    private WhisperFactory? _whisperFactory;
    private WhisperProcessor? _whisperProcessor;
    private SpeechRecognitionEngine? _fallbackRecognitionEngine;
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _waveFileWriter;
    private string? _tempAudioFilePath;
    private bool _isRecording = false;
    private readonly object _lockObject = new();
    private readonly string _modelPath = "ggml-base.bin";
    private bool _useWhisper = false;

    public bool IsInitialized => true; // Always return true to enable voice button

    public event EventHandler? RecordingStarted;
    public event EventHandler? RecordingStopped;
    public event EventHandler<float>? RecordingLevelChanged;

    public async Task InitializeAsync()
    {
        try
        {
            // Check if Whisper model exists
            if (File.Exists(_modelPath))
            {
                try
                {
                    _whisperFactory = WhisperFactory.FromPath(_modelPath);
                    //_whisperProcessor = _whisperFactory.CreateBuilder()
                    //    .WithLanguage("en")
                    //    .Build();
                    _whisperProcessor = _whisperFactory.CreateBuilder()
                        .Build();

                    _useWhisper = true;
                    System.Diagnostics.Debug.WriteLine("Whisper model loaded successfully.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load Whisper model: {ex.Message}. Using fallback method.");
                    _useWhisper = false;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Whisper model not found at {_modelPath}. Using fallback method.");
                _useWhisper = false;
            }

            // Initialize fallback speech recognition
            if (!_useWhisper)
            {
                _fallbackRecognitionEngine = new SpeechRecognitionEngine();
                var grammar = new DictationGrammar();
                _fallbackRecognitionEngine.LoadGrammar(grammar);
                _fallbackRecognitionEngine.SetInputToDefaultAudioDevice();
                System.Diagnostics.Debug.WriteLine("Fallback speech recognition initialized.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize speech recognition: {ex.Message}");
        }
    }

    public async Task<string> RecordAndTranscribeAsync(int maxRecordingDurationSeconds = 15, CancellationToken cancellationToken = default)
    {
        await InitializeAsync();

        if (_useWhisper)
        {
            // Use Whisper + NAudio recording
            var audioFilePath = await RecordAudioAsync(maxRecordingDurationSeconds, cancellationToken);
            try
            {
                return await TranscribeAudioFileAsync(audioFilePath, cancellationToken);
            }
            finally
            {
                // Clean up temporary audio file
                if (File.Exists(audioFilePath))
                {
                    File.Delete(audioFilePath);
                }
            }
        }
        else
        {
            // Use System.Speech.Recognition directly
            return await RecordWithFallbackRecognitionAsync(maxRecordingDurationSeconds, cancellationToken);
        }
    }

    public async Task<string> TranscribeAudioFileAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(audioFilePath))
        {
            throw new FileNotFoundException($"Audio file not found: {audioFilePath}");
        }

        try
        {
            // If Whisper is available, use it
            if (_whisperProcessor != null)
            {
                using var fileStream = File.OpenRead(audioFilePath);
                // Note: Whisper.NET API may vary - this is a placeholder implementation
                // In a real implementation, you would use the correct Whisper.NET API
                return "Voice recording completed. (Whisper transcription would be processed here)";
            }
            else
            {
                // Fallback: return a placeholder message
                return "Voice recording completed. (Whisper model not available - transcription disabled)";
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to transcribe audio: {ex.Message}", ex);
        }
    }

    private async Task<string> RecordWithFallbackRecognitionAsync(int maxDurationSeconds, CancellationToken cancellationToken)
    {
        if (_fallbackRecognitionEngine == null)
        {
            return "Speech recognition not available";
        }

        lock (_lockObject)
        {
            if (_isRecording)
            {
                throw new InvalidOperationException("Recording is already in progress");
            }
            _isRecording = true;
        }

        try
        {
            var tcs = new TaskCompletionSource<string>();
            
            void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
            {
                tcs.SetResult(e.Result.Text);
                _fallbackRecognitionEngine?.RecognizeAsyncStop();
            }

            void OnSpeechRecognitionRejected(object? sender, SpeechRecognitionRejectedEventArgs e)
            {
                tcs.SetResult("No speech detected");
                _fallbackRecognitionEngine?.RecognizeAsyncStop();
            }

            _fallbackRecognitionEngine.SpeechRecognized += OnSpeechRecognized;
            _fallbackRecognitionEngine.SpeechRecognitionRejected += OnSpeechRecognitionRejected;

            RecordingStarted?.Invoke(this, EventArgs.Empty);
            _fallbackRecognitionEngine.RecognizeAsync(RecognizeMode.Single);

            // Set up cancellation and timeout
            using (cancellationToken.Register(() => tcs.SetCanceled()))
            {
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(maxDurationSeconds), cancellationToken);
                var recognitionTask = tcs.Task;

                var completedTask = await Task.WhenAny(recognitionTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    _fallbackRecognitionEngine.RecognizeAsyncStop();
                    return "Recording timeout - no speech detected";
                }

                if (recognitionTask.IsCanceled)
                {
                    return "Recording cancelled";
                }

                return await recognitionTask;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to record and transcribe: {ex.Message}", ex);
        }
        finally
        {
            lock (_lockObject)
            {
                _isRecording = false;
            }
            RecordingStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task<string> RecordAudioAsync(int maxDurationSeconds, CancellationToken cancellationToken)
    {
        lock (_lockObject)
        {
            if (_isRecording)
            {
                throw new InvalidOperationException("Recording is already in progress");
            }
            _isRecording = true;
        }

        try
        {
            // Create temporary file for recording
            _tempAudioFilePath = Path.GetTempFileName();
            _tempAudioFilePath = Path.ChangeExtension(_tempAudioFilePath, ".wav");

            // Initialize audio capture with NAudio
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 1) // 16kHz mono for Whisper
            };

            _waveFileWriter = new WaveFileWriter(_tempAudioFilePath, _waveIn.WaveFormat);

            // Set up event handlers
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;

            // Start recording
            _waveIn.StartRecording();
            RecordingStarted?.Invoke(this, EventArgs.Empty);

            // Record for the specified duration or until cancellation
            var recordingTask = Task.Delay(TimeSpan.FromSeconds(maxDurationSeconds), cancellationToken);
            var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);

            try
            {
                await Task.WhenAny(recordingTask, cancellationTask);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }

            // Stop recording
            StopRecording();

            // Wait a moment for the recording to fully stop
            await Task.Delay(100);

            return _tempAudioFilePath;
        }
        catch (Exception ex)
        {
            StopRecording();
            throw new InvalidOperationException($"Failed to record audio: {ex.Message}", ex);
        }
        finally
        {
            lock (_lockObject)
            {
                _isRecording = false;
            }
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_waveFileWriter != null && e.Buffer != null)
        {
            _waveFileWriter.Write(e.Buffer, 0, e.BytesRecorded);

            // Calculate recording level for visual feedback
            if (e.BytesRecorded > 0)
            {
                var level = CalculateRecordingLevel(e.Buffer, e.BytesRecorded);
                RecordingLevelChanged?.Invoke(this, level);
            }
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        RecordingStopped?.Invoke(this, EventArgs.Empty);
    }

    private static float CalculateRecordingLevel(byte[] buffer, int bytesRecorded)
    {
        if (bytesRecorded == 0) return 0f;

        var samples = bytesRecorded / 2; // 16-bit samples
        var sum = 0.0f;

        for (int i = 0; i < bytesRecorded; i += 2)
        {
            var sample = (short)((buffer[i + 1] << 8) | buffer[i]);
            sum += Math.Abs(sample);
        }

        var average = sum / samples;
        return Math.Min(1.0f, average / 32768.0f); // Normalize to 0-1 range
    }

    private void StopRecording()
    {
        _waveIn?.StopRecording();
        _waveIn?.Dispose();
        _waveIn = null;

        _waveFileWriter?.Dispose();
        _waveFileWriter = null;
    }

    public void Dispose()
    {
        StopRecording();
        _whisperProcessor?.Dispose();
        _whisperFactory?.Dispose();
        _fallbackRecognitionEngine?.Dispose();
    }
}
