using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorAut.Services
{
    public interface IChatAzureService
    {
        Task<string> GetChatResponseAsync(string userMessage, string deploymentId);
        Task<List<string>> GetAvailableDeploymentsAsync();
    }

    public class ChatAzureService : IChatAzureService
    {
        private readonly HttpClient _httpClient;
        private readonly string _azureKey;
        private readonly string _azureEndpoint;
        private readonly string _apiVersion;

        public ChatAzureService(HttpClient httpClient, string azureEndpoint, string azureKey, string apiVersion = "2023-03-15-preview")
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _azureEndpoint = azureEndpoint ?? throw new ArgumentNullException(nameof(azureEndpoint));
            _azureKey = azureKey ?? throw new ArgumentNullException(nameof(azureKey));
            _apiVersion = apiVersion;

            _httpClient.BaseAddress = new Uri(_azureEndpoint);
            _httpClient.DefaultRequestHeaders.Add("api-key", _azureKey);
        }

        public async Task<string> GetChatResponseAsync(string userMessage, string deploymentId)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                throw new ArgumentException("User message cannot be empty.", nameof(userMessage));

            if (string.IsNullOrWhiteSpace(deploymentId))
                throw new ArgumentException("Deployment ID cannot be empty.", nameof(deploymentId));

            var requestBody = new
            {
                messages = new[]
               {
            new { role = "user", content = userMessage }
        },
                max_completion_tokens = 50000 // Заменено с max_tokens
            };


            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Формирование URL с deployment ID и версией API
            var requestUri = $"openai/deployments/{deploymentId}/chat/completions?api-version={_apiVersion}";

            var response = await _httpClient.PostAsync(requestUri, content);
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
                // Логирование ошибки или выброс исключения
                throw new HttpRequestException($"Error calling Azure OpenAI API: {response.StatusCode}, {responseString}");
            }
        }

        public async Task<List<string>> GetAvailableDeploymentsAsync()
        {
            var requestUri = $"openai/deployments?api-version={_apiVersion}";
            var response = await _httpClient.GetAsync(requestUri);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var deployments = new List<string>();
                using JsonDocument doc = JsonDocument.Parse(responseString);
                // Используем ключ "data" вместо "value"
                foreach (var deployment in doc.RootElement.GetProperty("data").EnumerateArray())
                {
                    // Изменяем "deploymentName" на "id" или "model" в зависимости от необходимости
                    var deploymentId = deployment.GetProperty("id").GetString();
                    // Или, если вам нужен "model":
                    // var model = deployment.GetProperty("model").GetString();

                    if (!string.IsNullOrEmpty(deploymentId))
                    {
                        deployments.Add(deploymentId);
                    }
                }
                return deployments;
            }
            else
            {
                throw new HttpRequestException($"Error fetching deployments from Azure OpenAI API: {response.StatusCode}, {responseString}");
            }
        }
    }
}
