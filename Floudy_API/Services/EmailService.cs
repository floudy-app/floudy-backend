using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Resend;

namespace Floudy.API.Services
{
    public class EmailService(IConfiguration configuration)
    {
        private readonly string senderEmail = configuration["ResendSenderEmail"] ?? "onboarding@resend.dev";
        private readonly string apiKey = configuration["ResendApiKey"] ?? throw new InvalidOperationException("ResendApiKey environment variable is not configured.");

        private class OptionsSnapshotWrapper<T> : IOptionsSnapshot<T> where T : class
        {
            public T Value { get; }
            public OptionsSnapshotWrapper(T value) => Value = value;
            public T Get(string? name) => Value;
        }

        public virtual async Task SendRecoveryEmail(string recipientEmail, string username, string resetUrl)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail)) throw new ArgumentException("Recipient email cannot be empty.", nameof(recipientEmail));

            var message = new EmailMessage();
            message.From = senderEmail;
            message.To.Add(recipientEmail);
            message.Subject = "Floudy Password Recovery";
            message.HtmlBody = $@"
                        <div style=""background: linear-gradient(135deg, #f5cba8 0%, #ffffff 100%); font-family: 'Nunito', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; padding: 40px 20px; min-height: 380px; color: #3d1a00;"">
                          <div style=""background: linear-gradient(160deg, #fde8d4, #fcd5d8); border-radius: 24px; padding: 40px 30px; max-width: 450px; margin: 0 auto; box-shadow: 0 8px 40px rgba(80, 30, 0, 0.13); text-align: center; border: 1px solid #ead5c0;"">
                            <h1 style=""font-size: 24px; font-weight: 900; margin-top: 0; margin-bottom: 10px; color: #3d1a00; text-transform: uppercase; letter-spacing: 0.05em;"">Password Recovery</h1>
                            <p style=""font-size: 15px; font-weight: 600; color: #8a5a3a; margin-bottom: 25px; line-height: 1.5;"">{username},<br/>There was a request to reset your password. If this was not you, ignore this.</p>
                            <div style=""margin: 30px 0;"">
                              <a href=""{resetUrl}"" style=""background: linear-gradient(90deg, #ff7bac, #ffb347); color: #ffffff; font-weight: 900; font-size: 14px; letter-spacing: 0.08em; text-decoration: none; border-radius: 24px; padding: 15px 35px; display: inline-block; box-shadow: 0 4px 16px rgba(255, 123, 172, 0.35); text-transform: uppercase;"">Reset Password</a>
                            </div>
                            <p style=""font-size: 12px; color: #c0a090; margin-top: 25px; margin-bottom: 0; line-height: 1.4;"">This reset link is valid for 5 minutes.</p>
                          </div>
                        </div>";

            var options = new OptionsSnapshotWrapper<ResendClientOptions>(new ResendClientOptions { ApiToken = apiKey });
            var resend = new ResendClient(options, new HttpClient { Timeout = TimeSpan.FromSeconds(10) });

            await resend.EmailSendAsync(message);
        }
    }
}
