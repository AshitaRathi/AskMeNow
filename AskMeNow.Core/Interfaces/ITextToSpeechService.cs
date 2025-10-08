namespace AskMeNow.Core.Interfaces
{
    public interface ITextToSpeechService
    {
        /// <summary>
        /// Converts text to speech and plays it through the default audio output device
        /// </summary>
        /// <param name="text">The text to convert to speech</param>
        /// <param name="cancellationToken">Cancellation token to stop speech</param>
        /// <returns>Task representing the speech operation</returns>
        Task SpeakAsync(string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts text to speech and saves it to an audio file
        /// </summary>
        /// <param name="text">The text to convert to speech</param>
        /// <param name="outputFilePath">Path where the audio file should be saved</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the speech operation</returns>
        Task SpeakToFileAsync(string text, string outputFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops any currently playing speech
        /// </summary>
        void StopSpeaking();

        /// <summary>
        /// Checks if speech is currently being played
        /// </summary>
        bool IsSpeaking { get; }

        /// <summary>
        /// Event fired when speech starts
        /// </summary>
        event EventHandler? SpeechStarted;

        /// <summary>
        /// Event fired when speech completes or is stopped
        /// </summary>
        event EventHandler? SpeechCompleted;

        /// <summary>
        /// Event fired when speech encounters an error
        /// </summary>
        event EventHandler<string>? SpeechError;
    }
}
