using AskMeNow.Core.Interfaces;
using System.Speech.Synthesis;

namespace AskMeNow.Infrastructure.Services
{
    public class SimpleTextToSpeechService : ITextToSpeechService, IDisposable
    {
        private SpeechSynthesizer? _speechSynthesizer;
        private bool _isSpeaking = false;

        public bool IsSpeaking => _isSpeaking;

        public event EventHandler? SpeechStarted;
        public event EventHandler? SpeechCompleted;
        public event EventHandler<string>? SpeechError;

        public SimpleTextToSpeechService()
        {
            InitializeService();
        }

        private void InitializeService()
        {
            try
            {
                _speechSynthesizer = new SpeechSynthesizer();
                _speechSynthesizer.SetOutputToDefaultAudioDevice();
                _speechSynthesizer.SpeakCompleted += OnSpeechCompleted;
                _speechSynthesizer.SpeakProgress += OnSpeechProgress;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize Text-to-Speech service: {ex.Message}", ex);
            }
        }

        public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (_isSpeaking)
            {
                StopSpeaking();
            }

            var tcs = new TaskCompletionSource<bool>();

            void OnCompleted(object? sender, SpeakCompletedEventArgs e)
            {
                tcs.SetResult(true);
            }

            try
            {
                _isSpeaking = true;
                SpeechStarted?.Invoke(this, EventArgs.Empty);

                _speechSynthesizer!.SpeakCompleted += OnCompleted;
                _speechSynthesizer.SpeakAsync(text);

                using (cancellationToken.Register(() => tcs.SetCanceled()))
                {
                    await tcs.Task;
                }
            }
            catch (Exception ex)
            {
                _isSpeaking = false;
                SpeechError?.Invoke(this, ex.Message);
                throw new InvalidOperationException($"Failed to speak text: {ex.Message}", ex);
            }
            finally
            {
                if (_speechSynthesizer != null)
                {
                    _speechSynthesizer.SpeakCompleted -= OnCompleted;
                }
            }
        }

        public async Task SpeakToFileAsync(string text, string outputFilePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                _speechSynthesizer!.SetOutputToWaveFile(outputFilePath);
                _speechSynthesizer.Speak(text);
                _speechSynthesizer.SetOutputToDefaultAudioDevice();
            }
            catch (Exception ex)
            {
                SpeechError?.Invoke(this, ex.Message);
                throw new InvalidOperationException($"Failed to save speech to file: {ex.Message}", ex);
            }
        }

        public void StopSpeaking()
        {
            if (_isSpeaking)
            {
                _speechSynthesizer?.SpeakAsyncCancelAll();
                _isSpeaking = false;
                SpeechCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnSpeechCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            _isSpeaking = false;
            SpeechCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void OnSpeechProgress(object? sender, SpeakProgressEventArgs e)
        {
        }

        public void Dispose()
        {
            StopSpeaking();
            _speechSynthesizer?.Dispose();
        }
    }
}