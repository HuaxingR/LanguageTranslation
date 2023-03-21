using Grpc.Net.Client;
using Server.Translation;
using System;
using System.CommandLine;

namespace Client.Translation
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var channel = GrpcChannel.ForAddress("http://localhost:5150");
            var client = new Server.Translation.Translation.TranslationClient(channel);
            string file = "Recording0002.wav";
            string recognizedLanguage = "";
            string translatedLanguage = "";
            var InputAudioPath = new Option<string>(
                name: "--input",
                description: "The path of input audio file to be translated.");
            var InputLanguage = new Option<string>(
                name: "--from",
                description: "The original language.");
            var OutputAudioPath = new Option<string>(
                name: "--to",
                description: "The target language.");
            var rootCommand = new RootCommand("Sample app for System.CommandLine");
            rootCommand.AddOption(InputAudioPath);
            rootCommand.AddOption(InputLanguage);
            rootCommand.AddOption(OutputAudioPath);
            rootCommand.SetHandler((path, inLang, outLang) => 
            { 
                file = path;
                recognizedLanguage = inLang;
                translatedLanguage = outLang;

            },
            InputAudioPath, InputLanguage, OutputAudioPath);
            await rootCommand.InvokeAsync(args);
            byte[] audioData = File.ReadAllBytes(file);
            Google.Protobuf.ByteString byteString = Google.Protobuf.ByteString.CopyFrom(audioData);

            Audio request = new Audio()
            {
                Data = byteString,
                FileType = "wav",
                Config = new TranslationConfig(){
                    RecognizedLanguage = recognizedLanguage,
                    TranslatedLanguage = translatedLanguage
                }
            };
            TextResponse transcribedText = await client.TranslateAudioToTextAsync(request);
            Console.WriteLine($"Transcribed Audio: {transcribedText.Text}");
            Audio dummy = await client.ConvertTextToAudio(transcribedText.Text);
            
        }
    }
}
