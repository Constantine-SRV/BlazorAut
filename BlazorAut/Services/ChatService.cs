using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorAut.Services
{
    public interface IChatService
    {
        Task<string> GetChatResponseAsync(string userMessage, string model);
        Task<List<string>> GetAvailableModelsAsync();
    }

    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly string _gptKey;

        public ChatService(HttpClient httpClient, string gptKey)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _gptKey = gptKey ?? throw new ArgumentNullException(nameof(gptKey), "GptKey не найден.");
        }

        public async Task<string> GetChatResponseAsync(string userMessage, string model)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                throw new ArgumentException("User message cannot be empty.", nameof(userMessage));

            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model cannot be empty.", nameof(model));

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = userMessage }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Set authorization header
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _gptKey);

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using JsonDocument doc = JsonDocument.Parse(responseString);
                var message = doc.RootElement
                                 .GetProperty("choices")[0]
                                 .GetProperty("message")
                                 .GetProperty("content")
                                 .GetString();
                return message?.Trim() ?? string.Empty;
            }
            else
            {
                // Log the error or throw exception
                throw new HttpRequestException($"Error calling OpenAI API: {response.StatusCode}, {responseString}");
            }
        }

        public async Task<List<string>> GetAvailableModelsAsync()
        {
            // Set authorization header
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _gptKey);

            var response = await _httpClient.GetAsync("https://api.openai.com/v1/models");
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var models = new List<string>();
                using JsonDocument doc = JsonDocument.Parse(responseString);
                foreach (var model in doc.RootElement.GetProperty("data").EnumerateArray())
                {
                    var modelId = model.GetProperty("id").GetString();
                    if (!string.IsNullOrEmpty(modelId))
                    {
                        models.Add(modelId);
                    }
                }
                return models;
            }
            else
            {
                // Log the error or throw exception
                throw new HttpRequestException($"Error fetching models from OpenAI API: {response.StatusCode}, {responseString}");
            }
        }
    }
}
