using MailKit.Net.Smtp;
using MimeKit;

namespace backend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("myExpenses", _config["Email:From"]));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Your myExpenses verification code";
            message.Body = new TextPart("plain")
            {
                Text = $"Your verification code is: {otpCode}\nThis code expires in 10 minutes."
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_config["Email:SmtpHost"], int.Parse(_config["Email:SmtpPort"]!), MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_config["Email:SmtpUser"], _config["Email:SmtpPass"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}