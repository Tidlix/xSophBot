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

            var safetySettings = new List<SafetySetting>()
            {
                new SafetySetting() { Category = HarmCategory.HARM_CATEGORY_HARASSMENT, Threshold = HarmBlockThreshold.BLOCK_ONLY_HIGH },
                new SafetySetting() { Category = HarmCategory.HARM_CATEGORY_HATE_SPEECH, Threshold = HarmBlockThreshold.BLOCK_MEDIUM_AND_ABOVE },
                new SafetySetting() { Category = HarmCategory.HARM_CATEGORY_TOXICITY, Threshold = HarmBlockThreshold.BLOCK_NONE },
                new SafetySetting() { Category = HarmCategory.HARM_CATEGORY_DANGEROUS, Threshold = HarmBlockThreshold.BLOCK_ONLY_HIGH },
                new SafetySetting() { Category = HarmCategory.HARM_CATEGORY_UNSPECIFIED, Threshold = HarmBlockThreshold.OFF }
            };

            var model = GoogleAI.CreateGenerativeModel("models/gemini-2.5-flash", safetyRatings: safetySettings);

            ThinkingConfig thinkConf = new ThinkingConfig
            {
                ThinkingBudget = 50
            };

            GenerationConfig genConf = new GenerationConfig
            {
                ThinkingConfig = thinkConf,
                Temperature = 2
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
