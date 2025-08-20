using GenerativeAI;
using GenerativeAI.Types;
using xSophBot.conf;


namespace xSophBot
{
    public class SGeminiEngine
    {
#pragma warning disable CS8618
        private static ChatSession Session;
        private static GoogleAi GoogleAI;
#pragma warning restore CS8618

        public static void StartSession()
        {
            GoogleAI = new GoogleAi(SConfig.AI.GeminiKey);

            var model = GoogleAI.CreateGenerativeModel("models/gemini-2.5-flash");

            ThinkingConfig thinkConf = new ThinkingConfig
            {
                ThinkingBudget = 0
            };

            GenerationConfig genConf = new GenerationConfig
            {
                ThinkingConfig = thinkConf
            };

            string SystemInstructions = SConfig.AI.SystemInstructions;


            Session = model.StartChat(config: genConf, systemInstruction: SystemInstructions);
        }

        public static async ValueTask<string> GenerateResponseAsync(string prompt)
        {
            var response = await Session.GenerateContentAsync(prompt);
            if (response.PromptFeedback?.BlockReason != null)
            {
                return $"[Blocked: {response.PromptFeedback.BlockReason}]";
            }
            return response.Text ?? string.Empty;
        }
    }
}
