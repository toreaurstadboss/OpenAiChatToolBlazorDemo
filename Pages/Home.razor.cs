using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAiChatToolBlazorDemo.Models;
using System.Net.Http.Json;
using System.Linq;
using OpenAI.Assistants;
using System.Text.Json;

namespace OpenAiChatToolBlazorDemo.Pages
{
    
    public partial class Home
    {

        protected string Question = string.Empty;
        protected string Message = string.Empty;

        private ChatClient _chatClient = default!;

        [Inject]
        public required OpenAIClient OpenAIClient { get; set; }

        [Inject]
        public required ModelSettings ModelSettings { get; set; }

        [Inject]
        public required HttpClient HttpClient { get; set; }

        protected override void OnInitialized()
        {
            _chatClient = OpenAIClient.GetChatClient(ModelSettings.ChatLanguageModelName);
        }

        private async Task OnEnterQuestion()
        {

            bool requiresAction = true;

            List<ChatMessage> messages = [new SystemChatMessage("""
                You are a sales bot, providing sales information at Jolly Travels to help 
                employees there to better understand their sales for the period 2024-2025 
                """),
                new UserChatMessage(Question)];
            ChatCompletionOptions options = new()
            {
                Tools = { getSalesInformationForGivenYearTool }
            };

            while (requiresAction)
            {
                requiresAction = false;
                ChatCompletion response = await _chatClient.CompleteChatAsync(messages, options);

                switch (response.FinishReason)
                {
                    case ChatFinishReason.ToolCalls:
                        {
                            messages.Add(new AssistantChatMessage(response));

                            foreach (ChatToolCall toolCall in response.ToolCalls)
                            {
                                switch (toolCall.FunctionName)
                                {
                                    case nameof(GetSalesInformationForGivenYear):
                                        {
                                            using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                            bool hasMonth = argumentsJson.RootElement.TryGetProperty("month", out JsonElement month);
                                            bool hasYear = argumentsJson.RootElement.TryGetProperty("year", out JsonElement year);

                                            if (!hasMonth)
                                            {
                                                throw new ArgumentNullException(nameof(month), "The month argument is required.");
                                            }

                                            if (!hasYear)
                                            {
                                                throw new ArgumentNullException(nameof(year), "The year argument is required.");
                                            }

                                            TravelData[] toolResult = await GetSalesInformationForGivenYear(month.GetString(), year.GetString());

                                            messages.Add(new ToolChatMessage(toolCall.Id, System.Text.Json.JsonSerializer.Serialize(toolResult, JsonSerializerOptions.Web)));
                                            break;
                                        }

                                    default:
                                        {
                                            throw new NotImplementedException();
                                        }
                                }
                            }

                            requiresAction = true;
                            break;
                        }

                    case ChatFinishReason.Stop:
                        {
                            messages.Add(new AssistantChatMessage(response));
                            break;
                        }

                    case ChatFinishReason.Length:
                        throw new NotImplementedException("Incomplete model output due to MaxTokens parameter or token limit exceeded.");

                    case ChatFinishReason.ContentFilter:
                        throw new NotImplementedException("Omitted content due to a content filter flag.");

                    default:
                        throw new NotImplementedException(response.FinishReason.ToString());
                }
            }

            foreach (ChatMessage message in messages)
            {
                if (message.Content.Count > 0)
                {
                    if (message is AssistantChatMessage)
                        Message += message.Content[0].Text;
                }
            }

        }

        private static readonly ChatTool getSalesInformationForGivenYearTool = ChatTool.CreateFunctionTool(
            functionName: nameof(GetSalesInformationForGivenYear),
            functionDescription: @"Get the travel sales for given year the agent sold. Return the information as a textual response
            ",
            functionParameters: BinaryData.FromString("""
                "type": "object",
                "properties": {
                    "month": {
                        "type": "string",
                        "description": "The month we want to get sales from"
                    },
                    "year": {
                        "type": "string",
                        "description": "The year we want to get sales from"

                    }
                },
                "required": [ "month", year" ]
                """)          
            );

        private async Task<TravelData[]> GetSalesInformationForGivenYear(string? month, string? year)
        {
            if (!int.TryParse(month, out int montParsed) || !int.TryParse(month, out int yearParsed))
            {
                return Array.Empty<TravelData>();
            }

            var travelData = await HttpClient.GetFromJsonAsync<TravelData[]>(@"sample-data\travelagency-salesdata.json");
            var fromTime = new DateTime(yearParsed, montParsed, 1);
            return travelData!.Where(td => td.TravelDate >= fromTime && td.TravelDate <= fromTime.AddDays(30)).ToArray();
        }

    }

}
