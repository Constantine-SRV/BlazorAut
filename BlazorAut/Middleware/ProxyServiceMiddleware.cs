using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlazorAut.Middleware
{
    public class ProxyServiceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProxyServiceMiddleware> _logger;
        private readonly List<string> _allowedDomains = new List<string>
        {
           // "google.com",
          //  "www.google.com",
          //  "microsoft.com",
          //  "www.microsoft.com",
            "example.com",
            "www.example.com",
            // Add other allowed domains
        };

        public ProxyServiceMiddleware(RequestDelegate next, ILogger<ProxyServiceMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _httpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = true, // Allow automatic redirects
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                // Temporarily disable SSL certificate validation for debugging (NOT RECOMMENDED FOR PRODUCTION)
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _logger.LogInformation("Incoming request: {Method} {Path}{QueryString}", context.Request.Method, context.Request.Path, context.Request.QueryString);

            if (context.Request.Path.StartsWithSegments("/proxy-service"))
            {
                var targetUrl = context.Request.Query["url"].ToString();
                _logger.LogInformation("Target URL: {TargetUrl}", targetUrl);

                if (string.IsNullOrEmpty(targetUrl))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("URL parameter is missing.");
                    _logger.LogWarning("URL parameter is missing.");
                    return;
                }

                if (!Uri.IsWellFormedUriString(targetUrl, UriKind.Absolute))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Invalid URL format.");
                    _logger.LogWarning("Invalid URL format.");
                    return;
                }

                var uri = new Uri(targetUrl);
                var isAllowed = _allowedDomains.Any(domain =>
                    uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase));

                if (!isAllowed)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Access to the specified domain is forbidden.");
                    _logger.LogWarning("Access to the domain '{Host}' is forbidden.", uri.Host);
                    return;
                }

                try
                {
                    // Create a new request to the target URL
                    var requestMessage = new HttpRequestMessage
                    {
                        Method = new HttpMethod(context.Request.Method),
                        RequestUri = uri
                    };

                    _logger.LogInformation("Proxying request to: {RequestUri}", requestMessage.RequestUri);

                    // Copy request headers, excluding Host
                    foreach (var header in context.Request.Headers)
                    {
                        if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // Skip Host header
                        }

                        if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                        {
                            requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                        }
                    }

                    // Set Host header according to the target domain
                    requestMessage.Headers.Host = uri.Host;

                    // Add User-Agent header if not already set
                    if (!requestMessage.Headers.Contains("User-Agent"))
                    {
                        requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                            "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
                    }

                    // Copy request body if present
                    if (!HttpMethods.IsGet(context.Request.Method) &&
                        !HttpMethods.IsHead(context.Request.Method) &&
                        !HttpMethods.IsDelete(context.Request.Method) &&
                        !HttpMethods.IsTrace(context.Request.Method))
                    {
                        requestMessage.Content = new StreamContent(context.Request.Body);
                        foreach (var header in context.Request.Headers)
                        {
                            if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                            {
                                requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                            }
                        }
                    }

                    // Send the request
                    _logger.LogInformation("Sending request to target URL: {TargetUrl}", targetUrl);
                    var responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
                    _logger.LogInformation("Received response: {StatusCode} {ReasonPhrase}", (int)responseMessage.StatusCode, responseMessage.ReasonPhrase);

                    // Copy response status code
                    context.Response.StatusCode = (int)responseMessage.StatusCode;

                    // Copy response headers
                    foreach (var header in responseMessage.Headers)
                    {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }

                    foreach (var header in responseMessage.Content.Headers)
                    {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }

                    // Remove headers that prevent embedding
                    context.Response.Headers.Remove("X-Frame-Options");
                    context.Response.Headers.Remove("Content-Security-Policy");
                    context.Response.Headers.Remove("X-Content-Type-Options");
                    context.Response.Headers.Remove("Referrer-Policy");
                    context.Response.Headers.Remove("Permissions-Policy");

                    // Copy response body
                    await responseMessage.Content.CopyToAsync(context.Response.Body);
                    _logger.LogInformation("Response successfully proxied.");
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync($"Error fetching the URL: {ex.Message}");
                    _logger.LogError(ex, "Error fetching the URL: {Message}", ex.Message);
                }

                return;
            }

            // Continue processing the request if the path does not start with /proxy-service
            await _next(context);
        }
    }
}
