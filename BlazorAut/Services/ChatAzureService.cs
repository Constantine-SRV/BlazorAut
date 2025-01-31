using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorAut.Services
{
    // Класс для хранения информации о лимитах
    public class RateLimitInfo
    {
        public int? Limit { get; set; }
        public int? RemainingRequests { get; set; }
        public int? RemainingTokens { get; set; }
        public DateTime? Reset { get; set; }
    }

    // Класс для объединения ответа GPT и информации о лимитах
    public class ChatResponse
    {
        public string Message { get; set; }
        public RateLimitInfo RateLimit { get; set; }
    }

    // Интерфейс сервиса ChatAzureService
    public interface IChatAzureService
    {
        Task<ChatResponse> GetChatResponseAsync(string userMessage, string deploymentId, int maxCompletionTokens = 5000);
        Task<List<string>> GetAvailableDeploymentsAsync();
        Task<RateLimitInfo> GetRateLimitInfoAsync(string deploymentId);
    }

    // Реализация интерфейса IChatAzureService
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
            if (!_httpClient.DefaultRequestHeaders.Contains("api-key"))
            {
                _httpClient.DefaultRequestHeaders.Add("api-key", _azureKey);
            }
        }

        /// <summary>
        /// Получает ответ от Azure OpenAI GPT на заданное сообщение с учетом контекста и максимального количества токенов.
        /// </summary>
        /// <param name="userMessage">Сообщение пользователя.</param>
        /// <param name="deploymentId">ID развертывания модели.</param>
        /// <param name="maxCompletionTokens">Максимальное количество токенов в ответе (по умолчанию 5000).</param>
        /// <returns>Ответ GPT и информация о лимитах.</returns>
        public async Task<ChatResponse> GetChatResponseAsync(string userMessage, string deploymentId, int maxCompletionTokens = 5000)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                throw new ArgumentException("User message cannot be empty.", nameof(userMessage));

            if (string.IsNullOrWhiteSpace(deploymentId))
                throw new ArgumentException("Deployment ID cannot be empty.", nameof(deploymentId));

            if (maxCompletionTokens <= 0)
                throw new ArgumentException("maxCompletionTokens must be a positive integer.", nameof(maxCompletionTokens));

            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "user", content = userMessage }
                },
                max_completion_tokens = maxCompletionTokens
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestUri = $"openai/deployments/{deploymentId}/chat/completions?api-version={_apiVersion}";

            HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);
            var responseString = await response.Content.ReadAsStringAsync();

            var rateLimitInfo = ExtractRateLimitInfo(response.Headers);

            if (response.IsSuccessStatusCode)
            {
                using JsonDocument doc = JsonDocument.Parse(responseString);
                var message = doc.RootElement
                                 .GetProperty("choices")[0]
                                 .GetProperty("message")
                                 .GetProperty("content")
                                 .GetString();
                return new ChatResponse
                {
                    Message = message?.Trim() ?? string.Empty,
                    RateLimit = rateLimitInfo
                };
            }
            else
            {
                throw new HttpRequestException($"Error calling Azure OpenAI API: {response.StatusCode}, {responseString}");
            }
        }

        /// <summary>
        /// Получает список доступных развертываний моделей Azure OpenAI.
        /// </summary>
        /// <returns>Список ID развертываний.</returns>
        public async Task<List<string>> GetAvailableDeploymentsAsync()
        {
            var requestUri = $"openai/deployments?api-version={_apiVersion}";
            var response = await _httpClient.GetAsync(requestUri);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var deployments = new List<string>();
                using JsonDocument doc = JsonDocument.Parse(responseString);
                foreach (var deployment in doc.RootElement.GetProperty("data").EnumerateArray())
                {
                    var deploymentId = deployment.GetProperty("id").GetString();
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

        /// <summary>
        /// Выполняет тестовый запрос для получения информации о текущих лимитах.
        /// </summary>
        /// <param name="deploymentId">ID развертывания модели.</param>
        /// <returns>Информация о текущих лимитах.</returns>
        public async Task<RateLimitInfo> GetRateLimitInfoAsync(string deploymentId)
        {
            if (string.IsNullOrWhiteSpace(deploymentId))
                throw new ArgumentException("Deployment ID cannot be empty.", nameof(deploymentId));

            // Отправляем тестовый запрос с минимальным количеством токенов
            var testMessage = "Rate limit check.";
            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "user", content = testMessage }
                },
                max_completion_tokens = 1 // Минимальное количество токенов
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestUri = $"openai/deployments/{deploymentId}/chat/completions?api-version={_apiVersion}";

            HttpResponseMessage response = await _httpClient.PostAsync(requestUri, content);
            var responseString = await response.Content.ReadAsStringAsync();

            var rateLimitInfo = ExtractRateLimitInfo(response.Headers);

            // Можно обработать ответ, если необходимо
            if (response.IsSuccessStatusCode)
            {
                return rateLimitInfo;
            }
            else
            {
                throw new HttpRequestException($"Error checking rate limits: {response.StatusCode}, {responseString}");
            }
        }

        /// <summary>
        /// Извлекает информацию о лимитах из заголовков ответа.
        /// </summary>
        /// <param name="headers">Заголовки ответа HTTP.</param>
        /// <returns>Информация о лимитах.</returns>
        private RateLimitInfo ExtractRateLimitInfo(System.Net.Http.Headers.HttpResponseHeaders headers)
        {
            var rateLimitInfo = new RateLimitInfo();

            if (headers.TryGetValues("x-ratelimit-limit-requests", out var limitRequestsValues))
            {
                if (int.TryParse(limitRequestsValues.FirstOrDefault(), out int limitRequests))
                {
                    rateLimitInfo.Limit = limitRequests;
                }
            }

            if (headers.TryGetValues("x-ratelimit-remaining-requests", out var remainingRequestsValues))
            {
                if (int.TryParse(remainingRequestsValues.FirstOrDefault(), out int remainingRequests))
                {
                    rateLimitInfo.RemainingRequests = remainingRequests;
                }
            }

            if (headers.TryGetValues("x-ratelimit-remaining-tokens", out var remainingTokensValues))
            {
                if (int.TryParse(remainingTokensValues.FirstOrDefault(), out int remainingTokens))
                {
                    rateLimitInfo.RemainingTokens = remainingTokens;
                }
            }

            if (headers.TryGetValues("x-ratelimit-reset", out var resetValues))
            {
                if (long.TryParse(resetValues.FirstOrDefault(), out long resetUnix))
                {
                    rateLimitInfo.Reset = DateTimeOffset.FromUnixTimeSeconds(resetUnix).UtcDateTime;
                }
            }

            return rateLimitInfo;
        }
    }
}
