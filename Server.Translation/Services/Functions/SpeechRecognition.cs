using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using NAudio.Wave;

namespace Server.Translation
{
    class SpeechRecognition
    {
        private readonly SpeechConfig _config;
        private readonly SpeechTranslationConfig _translationConfig;

        public SpeechRecognition(Secrets secrets)
        {
            _config = SpeechConfig.FromSubscription(secrets.Key, secrets.Endpoint);
            _translationConfig = SpeechTranslationConfig.FromSubscription(secrets.Key, secrets.Endpoint);

        }

        public int OutputSpeechSynthesisResult(SpeechSynthesisResult speechSynthesisResult, string text)
        {
            switch (speechSynthesisResult.Reason)
            {
                case ResultReason.SynthesizingAudioCompleted:
                    Console.WriteLine($"Speech synthesized for text: [{text}]");
                    break;
                case ResultReason.Canceled:
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                        Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                    }
                    break;
                default:
                    break;
            }
            return 1;
        }

        public void ConvertFileFormatToWave(string outputFileName, string inputFileName)
        {
            if (!File.Exists(inputFileName))
            {
                Console.WriteLine("File does not exist");
            }

            using (var reader = new MediaFoundationReader(inputFileName))
            {
                WaveFileWriter.CreateWaveFile(outputFileName, reader);
            }
        }

        public void ConvertByteArrayToWave(string outputFile, byte[] audioBytes)
        {
            using (var outputStream = new MemoryStream())
            using (var writer = new WaveFileWriter(outputStream, new WaveFormat()))
            {
                writer.Write(audioBytes, 0, audioBytes.Length);
                writer.Flush();
                outputStream.Seek(0, SeekOrigin.Begin);
                File.WriteAllBytes(outputFile, outputStream.ToArray());
            }
        }

        public async Task<TranslationRecognitionResult> TranslateFromAudioFile(string file, string translatedLanguage, string recognizedLanguage = "")
        {
            if (!File.Exists(file))
            {
                throw new InvalidDataException();
            }

            // Configure the recognized language
            if (string.IsNullOrEmpty(recognizedLanguage))
            {
                _translationConfig.SpeechRecognitionLanguage = "en-US";
            }
            else
            {
                // TODO: Ensure recognized language it's one of the supported languages
                _translationConfig.SpeechRecognitionLanguage = recognizedLanguage;
            }

            // TODO: Ensure the translated language it's one of the supported languages
            _translationConfig.AddTargetLanguage(translatedLanguage);

            using var audioConfig = AudioConfig.FromWavFileInput(file);
            using var translationRecognizer = new TranslationRecognizer(_translationConfig, audioConfig);

            var translationRecognitionResult = await translationRecognizer.RecognizeOnceAsync();

            return translationRecognitionResult;
        }

        public async Task<SpeechRecognitionResult> TranscribeFromAudioFile(string file, string recognizedLanguage = "")
        {
            if (!File.Exists(file))
            {
                throw new InvalidDataException();
            }

            // Configure audio input format
            using var audioConfig = AudioConfig.FromWavFileInput(file);

            // Configure recognition targeting language
            // Default recognition targeting language to English
            if (string.IsNullOrEmpty(recognizedLanguage))
            {
                _config.SpeechRecognitionLanguage = "en-US";
            }
            else
            {
                _config.SpeechRecognitionLanguage = recognizedLanguage;
            }

            // Initialize the speech recognizer
            using var speechRecognizer = new SpeechRecognizer(_config, audioConfig);
            var result = await speechRecognizer.RecognizeOnceAsync();

            return result;
        }
    }
}