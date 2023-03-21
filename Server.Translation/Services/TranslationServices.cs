using Grpc.Core;
using Microsoft.CognitiveServices.Speech;

namespace Server.Translation
{
    public class TranslationServices : Translation.TranslationBase
    {
        private readonly ILogger<TranslationServices> _logger;

        private readonly SpeechRecognition _recognition;

        static string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
        static string speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION");

        public TranslationServices(ILogger<TranslationServices> logger, Secrets secrets)
        {
            _logger = logger;
            _recognition = new SpeechRecognition(secrets);
        }

        // Currently only transcribe audio files in "Wav" format
        public override async Task<TextResponse> TranscribeAudio(Audio request, ServerCallContext context)
        {
            _logger.LogInformation($"Received a {nameof(TranscribeAudio)}request");

            // check format
            if (request.FileType != "wav")
            {
                _logger.LogError($"audio file type is unsupported {request.FileType}");
                return new TextResponse()
                {
                    Text = "Media unsupported",
                    Recognized = false
                };
            }

            if (request.Config == null)
            {
                _logger.LogError($"{nameof(request.Config)} is null");
                return new TextResponse()
                {
                    Text = "Untranscribed",
                    Recognized = false
                };
            }

            // convert audio data into byte array
            var data = request.Data.ToByteArray();

            // write data to a file
            string outputFile = "output.wav";
            File.WriteAllBytes(outputFile, data);
            _logger.LogInformation("Sucessfully written data to a file");

            // set up return result
            TextResponse response = new TextResponse()
            {
                Text = "Untranscribed",
                Recognized = false
            };

            if (string.IsNullOrEmpty(request.Config.RecognizedLanguage))
            {
                response.Language = "en-US";
            }
            else
            {
                response.Language = request.Config.RecognizedLanguage;
            }

            try
            {
                SpeechRecognitionResult result = await _recognition.TranscribeFromAudioFile(
                                                            outputFile,
                                                            request.Config.RecognizedLanguage).ConfigureAwait(false);

                switch (result.Reason)
                {
                    case ResultReason.RecognizedSpeech:
                        response.Text = result.Text;
                        response.Recognized = true;
                        break;
                    case ResultReason.NoMatch:
                        response.Text = "Speech could not be recognized";
                        response.Recognized = false;
                        break;
                    case ResultReason.Canceled:
                        var cancellation = CancellationDetails.FromResult(result);
                        _logger.LogInformation($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            _logger.LogError($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            _logger.LogError($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                            _logger.LogError($"CANCELED: Did you set the speech resource key and region values?");
                        }

                        response.Text = "Transcription is cancelled";
                        response.Recognized = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                response.Text = "Internal server error";
                response.Recognized = false;
                _logger.LogError(ex, "An exception is thrown while transcribing audio");
            }

            // Delete the file written to disk
            File.Delete(outputFile);

            return response;
        }

        public override async Task<TextResponse> TranslateAudioToText(Audio request, ServerCallContext context)
        {
            _logger.LogInformation($"Received a {nameof(TranslateAudioToText)}request");

            // check format
            if (request.FileType != "wav")
            {
                _logger.LogError($"audio file type is unsupported {request.FileType}");
                return new TextResponse()
                {
                    Text = "Media unsupported",
                    Recognized = false
                };
            }

            if (request.Config == null)
            {
                _logger.LogError($"{nameof(request.Config)} is null");
                return new TextResponse()
                {
                    Text = "Untranscribed",
                    Recognized = false
                };
            }

            // convert audio data into byte array
            var data = request.Data.ToByteArray();

            // write data to a file
            string outputFile = "output.wav";
            File.WriteAllBytes(outputFile, data);
            _logger.LogInformation("Sucessfully written data to a file");

            TextResponse response = new TextResponse()
            {
                Text = "Not translated",
                Recognized = false
            };

            try
            {

                var result = await _recognition.TranslateFromAudioFile(
                                                    outputFile,
                                                    request.Config.TranslatedLanguage,
                                                    request.Config.RecognizedLanguage).ConfigureAwait(false);

                switch (result.Reason)
                {
                    case ResultReason.TranslatedSpeech:
                        // Console.WriteLine($"RECOGNIZED: Text={translationRecognitionResult.Text}");
                        response.Text = string.Empty;
                        foreach (var element in result.Translations)
                        {
                            response.Text += element.Value;
                        }
                        response.Recognized = true;
                        break;
                    case ResultReason.NoMatch:
                        _logger.LogError($"NOMATCH: Speech could not be recognized.");
                        response.Text = "Unrecognized speech";
                        response.Recognized = false;
                        break;
                    case ResultReason.Canceled:
                        var cancellation = CancellationDetails.FromResult(result);
                        _logger.LogError($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            _logger.LogError($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            _logger.LogError($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                            _logger.LogError($"CANCELED: Did you set the speech resource key and region values?");
                        }

                        response.Text = "Canceled translation";
                        response.Recognized = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                response.Text = "Internal server error";
                response.Recognized = false;
                _logger.LogError(ex, "An exception is thrown while transcribing audio");
            }

            return response;
        }

        public override async Task<Audio> ConvertTextToAudio(TextResponse request)
        {
            _logger.LogInformation($"Received a {nameof(ConvertTextToAudio)}request");
            try
            {
                var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);      

                // The language of the voice that speaks.
                speechConfig.SpeechSynthesisVoiceName = "en-US-JennyNeural"; 
                var speechSynthesizer = new SpeechSynthesizer(speechConfig);
                var speechSynthesisResult = await speechSynthesizer.SpeakTextAsync(request.Text);
                _recognition.OutputSpeechSynthesisResult(speechSynthesisResult, request.Text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception is thrown while transcribing audio");
            }
            Audio dummy = new Audio();
            return dummy;

        }

    }
}