using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorAut.Data;

namespace BlazorAut.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }

    public class GraphEmailSender : IEmailSender
    {
        private readonly IConfidentialClientApplication _clientApplication;
        private readonly AzureAdOptions _azureAdOptions;

        public GraphEmailSender(ApplicationDbContext dbContext)
        {
            
            _azureAdOptions = dbContext.AzureAdOptions.FirstOrDefault() ?? throw new InvalidOperationException("Azure AD options not configured.");

            _clientApplication = ConfidentialClientApplicationBuilder.Create(_azureAdOptions.ClientId)
                .WithClientSecret(_azureAdOptions.ClientSecret)
                .WithAuthority(new Uri($"{_azureAdOptions.Instance}{_azureAdOptions.TenantId}"))
                .Build();
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var scopes = new[] { "https://graph.microsoft.com/.default" };
            var authResult = await _clientApplication.AcquireTokenForClient(scopes).ExecuteAsync();

            var tokenCredential = new ClientSecretCredential(
                _azureAdOptions.TenantId, _azureAdOptions.ClientId, _azureAdOptions.ClientSecret);

            var graphClient = new GraphServiceClient(tokenCredential);

            var emailMessage = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Text,
                    Content = message
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = email
                        }
                    }
                }
            };

            var sendMailRequest = new SendMailPostRequestBody
            {
                Message = emailMessage,
                SaveToSentItems = true
            };

            await graphClient.Users[_azureAdOptions.SenderUserId].SendMail.PostAsync(sendMailRequest);
        }
    }
}
