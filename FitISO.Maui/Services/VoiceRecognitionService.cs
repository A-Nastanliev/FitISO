using System.Globalization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;

namespace FitISO.Maui.Services
{
    public class VoiceRecognitionService
    {
        static readonly TimeSpan MaxListenTime = TimeSpan.FromSeconds(12);
        static readonly TimeSpan NativeCallTimeout = TimeSpan.FromSeconds(2);

        ISpeechToText? current;

        public async Task<string?> ListenOnceAsync(CultureInfo culture, CancellationToken cancellationToken = default)
        {
            if (current is not null) return null; 

            var speechToText = new SpeechToTextImplementation();
            current = speechToText;

            bool recognizerFinished = false;
            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs e)
            {
                recognizerFinished = true;
                var result = e.RecognitionResult;

                if (!result.IsSuccessful)
                {
                    System.Diagnostics.Debug.WriteLine($"Speech recognition error: {result.Exception}");
#if DEBUG
                    if (!cancellationToken.IsCancellationRequested)
                        _ = Toast.Make($"Voice error: {result.Exception?.Message ?? "unknown"}").Show();
#endif
                }

                tcs.TrySetResult(result.IsSuccessful ? result.Text : null);
            }

            try
            {
                var mic = await Permissions.RequestAsync<Permissions.Microphone>();
                bool speech = await speechToText.RequestPermissions(CancellationToken.None);

                if (mic != PermissionStatus.Granted || !speech)
                {
                    _ = Toast.Make("Microphone / speech permission is required for voice logging").Show();
                    return null;
                }

                speechToText.RecognitionResultCompleted += OnCompleted;

                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linked.CancelAfter(MaxListenTime);
                using var registration = linked.Token.Register(() => tcs.TrySetResult(null));

                await speechToText.StartListenAsync(new SpeechToTextOptions
                {
                    Culture = culture,
                    ShouldReportPartialResults = false
                }, CancellationToken.None);

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Speech recognition failed: {ex}");
#if DEBUG
                _ = Toast.Make($"Voice error: {ex.Message}").Show();
#endif
                return null;
            }
            finally
            {
                speechToText.RecognitionResultCompleted -= OnCompleted;

                if (!recognizerFinished)
                    await StopQuietlyAsync(speechToText);

                await DisposeQuietlyAsync(speechToText);
                current = null;
            }
        }

        public Task StopAsync() => current is { } s ? StopQuietlyAsync(s) : Task.CompletedTask;

        static async Task StopQuietlyAsync(ISpeechToText speechToText)
        {
            try
            {
                if (speechToText.CurrentState != SpeechToTextState.Listening) return;
                await Task.WhenAny(speechToText.StopListenAsync(CancellationToken.None), Task.Delay(NativeCallTimeout));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Stopping speech recognition failed: {ex}");
            }
        }

        static async Task DisposeQuietlyAsync(ISpeechToText speechToText)
        {
            try
            {
                await Task.WhenAny(speechToText.DisposeAsync().AsTask(), Task.Delay(NativeCallTimeout));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Disposing speech recognition failed: {ex}");
            }
        }
    }
}