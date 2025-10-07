using System.ComponentModel;
using TwitchSharp.Entitys;
using GenerativeAI;
using GenerativeAI.Types;
using GenerativeAI.Tools;
using xSophBot.conf;

namespace GeminiTest
{
    public class GeminiEngine
    {
#pragma warning disable CS8618
        public static GoogleAi GoogleAI { get; private set; }
        public static GenerativeModel MainModel { get; private set; }
        public static GenerativeModel GoogleModel { get; private set; }
        public static ChatSession MainChat { get; private set; }
        public static ChatSession GoogleChat { get; private set; }
        private static string MemoryFile = $"{AppDomain.CurrentDomain.BaseDirectory}/config/ai.memory";
#pragma warning restore CS8618

        public static void Initialize(string token)
        {
            GoogleAI = new GoogleAi(token);
            MainModel = GoogleAI.CreateGenerativeModel("models/gemini-2.5-flash");
            GoogleModel = GoogleAI.CreateGenerativeModel("models/gemini-2.5-flash");

            MainModel.UseGoogleSearch = false;
            MainModel.SystemInstruction = SConfig.AI.SystemInstructions;
            MainModel.EnableFunctions();
            MainModel.AddFunctionTool(new QuickTool(() => ReadMemory(), "ReadMemory", "Lies die aktuelle Erinnerungsdatei"));
            MainModel.AddFunctionTool(new QuickTool((string addLine) => AddMemory(addLine), "AddMemory", "Schreibe eine neue Zeile in deine Erinnerungsdatei"));
            MainModel.AddFunctionTool(new QuickTool((string from, string to) => ModifyMemory(from, to), "ModifyMemory", "Bearbeite eine Zeile in deiner Erinnerungsdatei"));

            GoogleModel.UseGoogleSearch = true;

            StartNewMainChat();
            StartNewGoogleChat();
        }

        public static void StartNewMainChat()
        {
            MainChat = MainModel.StartChat();
        }
        public static void StartNewGoogleChat()
        {
            GoogleChat = GoogleModel.StartChat();
        }

        public static async Task<string> GenerateResponseAsync(AiRequest request)
        {
            return (await MainChat.GenerateContentAsync(request.ToString())).Text;
        }



        private static string ReadMemory()
        {
            Console.WriteLine("Reading Memory...");
            return File.ReadAllText(MemoryFile);
        }
        private static void AddMemory(string line)
        {
            Console.WriteLine("Writing Memory...");
            File.AppendAllText(MemoryFile, "\n" + line);
        }
        private static void ModifyMemory(string oldLine, string newLine)
        {
            Console.WriteLine("Modifying Memory...");
            string current = ReadMemory();
            File.WriteAllText(MemoryFile, current.Replace(oldLine, newLine));
        }
    }
    public class AiRequest
    {
        public SourceType Source;
        public string Name { get; private set; }
        public string Id { get; private set; }
        public string Promt { get; private set; }
        public bool IsPrivate { get; private set; }

        public AiRequest(TwitchUser user, string promt, bool isPrivate)
        {
            Source = SourceType.Twitch;
            Id = user.ID;
            Name = user.DisplayName;
            Promt = promt;
            IsPrivate = isPrivate;
        }
        public AiRequest(string promt)
        {
            Source = SourceType.Console;
            Id = "0";
            Name = "Console";
            Promt = promt;
            IsPrivate = true;
        }

#pragma warning disable CS0114
        public string ToString()
        {
            // [DateTime] {Name} (id={id}) schreibt über {Plattform} {Optional: (Im Privaten)}: {Promt}
            return $"[{DateTime.Now.ToString("dd.MM.yyyy - HH:mm:ss")}] {Name} (id={Id}) schreibt über {Source}{(IsPrivate ? "( Im Privaten)" : "")}: {Promt}";
        }
#pragma warning restore CS0114 

        public enum SourceType
        {
            Twitch,
            Discord,
            Console
        }
    }    
}
