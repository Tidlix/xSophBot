using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenerativeAI;
using GenerativeAI.Clients;
using GenerativeAI.Types;
using xSophBot.bot.conf;
using xSophBot.bot.logs;

namespace xSophBot.bot.ai
{
    public class xSGeminiEngine
    {
#pragma warning disable CS8618
        private static ChatSession Session;
        private static GoogleAi GoogleAI;
#pragma warning restore CS8618

        public static async Task StartSession()
        {
            GoogleAI = new GoogleAi(xSConfig.AI.GeminiKey);

            var model = GoogleAI.CreateGenerativeModel("models/gemini-2.5-flash");

            ThinkingConfig thinkConf = new ThinkingConfig
            {
                ThinkingBudget = 0
            };

            GenerationConfig genConf = new GenerationConfig
            {
                ThinkingConfig = thinkConf
            };

            string SystemInstructions = xSConfig.AI.SystemInstructions;

            Session = model.StartChat(config: genConf, systemInstruction: SystemInstructions);
        }

        public static async ValueTask<string> GenerateResponseAsync(string prompt)
        {
            var response = await Session.GenerateContentAsync(prompt);
            return response.Text ?? string.Empty;
        }
    }
}
