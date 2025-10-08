using AskMeNow.Core.Interfaces;
using NAudio.Wave;
using System.Speech.Recognition;

namespace AskMeNow.Infrastructure.Services
{
    public class SimpleSpeechToTextService : ISpeechToTextService, IDisposable
    {
        private SpeechRecognitionEngine? _speechRecognitionEngine;
        private WaveInEvent? _waveIn;
        private WaveFileWriter? _waveFileWriter;
        private string? _tempAudioFilePath;
        private bool _isRecording = false;
        private readonly object _lockObject = new();
        private TaskCompletionSource<string>? _recognitionTaskCompletionSource;

        public bool IsInitialized => _speechRecognitionEngine != null;

        public event EventHandler? RecordingStarted;
        public event EventHandler? RecordingStopped;
        public event EventHandler<float>? RecordingLevelChanged;

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            try
            {
                _speechRecognitionEngine = new SpeechRecognitionEngine();

                var grammar = new DictationGrammar();
                _speechRecognitionEngine.LoadGrammar(grammar);

                _speechRecognitionEngine.SpeechRecognized += OnSpeechRecognized;
                _speechRecognitionEngine.SpeechRecognitionRejected += OnSpeechRecognitionRejected;

                _speechRecognitionEngine.SetInputToDefaultAudioDevice();

                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize speech recognition: {ex.Message}", ex);
            }
        }

        public async Task<string> RecordAndTranscribeAsync(int maxRecordingDurationSeconds = 15, CancellationToken cancellationToken = default)
        {
            if (!IsInitialized)
            {
                await InitializeAsync();
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
                _recognitionTaskCompletionSource = new TaskCompletionSource<string>();

                _speechRecognitionEngine!.RecognizeAsync(RecognizeMode.Single);
                RecordingStarted?.Invoke(this, EventArgs.Empty);

                using (cancellationToken.Register(() => _recognitionTaskCompletionSource?.SetCanceled()))
                {
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(maxRecordingDurationSeconds), cancellationToken);
                    var recognitionTask = _recognitionTaskCompletionSource.Task;

                    var completedTask = await Task.WhenAny(recognitionTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        _speechRecognitionEngine.RecognizeAsyncStop();
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

        public async Task<string> TranscribeAudioFileAsync(string audioFilePath, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult("Audio file transcription not implemented in simple mode");
        }

        private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            _recognitionTaskCompletionSource?.SetResult(e.Result.Text);
            _speechRecognitionEngine?.RecognizeAsyncStop();
        }

        private void OnSpeechRecognitionRejected(object? sender, SpeechRecognitionRejectedEventArgs e)
        {
            _recognitionTaskCompletionSource?.SetResult("No speech detected");
            _speechRecognitionEngine?.RecognizeAsyncStop();
        }

        public void Dispose()
        {
            _speechRecognitionEngine?.Dispose();
            _waveIn?.Dispose();
            _waveFileWriter?.Dispose();
        }
    }
}