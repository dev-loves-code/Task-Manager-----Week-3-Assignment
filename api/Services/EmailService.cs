using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Interfaces;
using DotNetEnv;
using MailKit.Net.Smtp;
using MimeKit;

namespace api.Services
{
    public class EmailService : IEmailService
    {
        public async Task SendWelcomeEmailAsync(string email, string username)
        {
            Env.Load();

            var emailSender = Environment.GetEnvironmentVariable("email");
            var password = Environment.GetEnvironmentVariable("password");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Task-Manager", emailSender));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Welcome to Task-Manager!";

            string body = $@"
            <h2>Welcome, {username}!</h2>
            <p>Thanks for registering at Task-Manager. We're glad to have you.</p>";


            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(emailSender, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }


        public async Task SendReportEmailAsync(string email, string username, string tempPath)
        {
            var emailSender = Environment.GetEnvironmentVariable("email");
            var password = Environment.GetEnvironmentVariable("password");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Task-Manager", emailSender));
            message.To.Add(new MailboxAddress(username, email));
            message.Subject = "Your Weekly Task Report 📋";

            var body = $@"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #4a90e2;'>Hello {username}! 👋</h2>
        <p>Your weekly task report is ready! 📊</p>
        <p>This report contains:</p>
        <ul>
            <li>⚠️ Past due tasks from last week</li>
            <li>🚀 Upcoming tasks for next week</li>
        </ul>
        <p>Keep up the great work and stay organized! 💪</p>
        <hr style='border: 1px solid #eee; margin: 20px 0;'>
        <p style='font-size: 12px; color: #666;'>
            Generated on {DateTime.Now:MMM dd, yyyy 'at' HH:mm} | Task-Manager
        </p>
    </div>
</body>
</html>";

            var multipart = new Multipart("mixed")
    {
        new TextPart("html") { Text = body }
    };


            if (File.Exists(tempPath))
            {
                multipart.Add(new MimePart("application", "pdf")
                {
                    Content = new MimeContent(File.OpenRead(tempPath), ContentEncoding.Default),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = Path.GetFileName(tempPath)
                });
            }

            message.Body = multipart;

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(emailSender, password);
                await client.SendAsync(message);
            }
            finally
            {
                await client.DisconnectAsync(true);


                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

    }
}