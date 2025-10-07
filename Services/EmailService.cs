using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace UserManagement.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string userName, string callbackUrl);
}
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }
    public async Task SendVerificationEmailAsync(string email, string userName, string callbackUrl)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("=== EMAIL SENDING STARTED ===");
                _logger.LogInformation($"Attempting to send verification email to: {email}");

                var emailPassword = _configuration["EmailSettings:Password"];
                
                if (string.IsNullOrEmpty(emailPassword))
                {
                    _logger.LogError("EMAIL PASSWORD IS NOT CONFIGURED!");
                    return;
                }

                _logger.LogInformation($"Email password configured: {emailPassword.Substring(0, 3)}***");
                
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("User Management System", "itransition5@goksu.me"));
                message.To.Add(new MailboxAddress(userName, email));
                message.Subject = "Verify Your Email Address";

                message.Body = new TextPart("html")
                {
                    Text = $@"
                        <h2>Hello {userName},</h2>
                        <p>Thank you for registering. Please verify your email by clicking the link below:</p>
                        <p><a href='{callbackUrl}'>Verify Email</a></p>
                        <p>If you did not register, please ignore this email.</p>
                    "
                };

                _logger.LogInformation("Connecting to SMTP server: mail.goksu.me:465");

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync("mail.goksu.me", 465, SecureSocketOptions.SslOnConnect);
                    
                    _logger.LogInformation("Connected to SMTP server. Authenticating...");
                    
                    await client.AuthenticateAsync("itransition5@goksu.me", emailPassword);
                    
                    _logger.LogInformation("Authenticated. Sending email...");
                    
                    await client.SendAsync(message);
                    
                    _logger.LogInformation("Email sent. Disconnecting...");
                    
                    await client.DisconnectAsync(true);
                    
                    _logger.LogInformation($"✅ EMAIL SENT SUCCESSFULLY to: {email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ FAILED TO SEND EMAIL to {email}");
                _logger.LogError($"Error: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
            }
        });
        
        await Task.CompletedTask;
    }
}
