using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace eSKHub.Services
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string toEmail, string otpCode);
        Task SendYouthOtpEmailAsync(string toEmail, string otpCode, string youthName);
        Task SendFeedbackOtpEmailAsync(string toEmail, string otpCode, string youthName);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                emailSettings["SenderName"],
                emailSettings["SenderEmail"]
            ));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Password Reset - Verification Code";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Password Reset Verification</h2>
                    <p>Your verification code is:</p>
                    <h1 style='color: #7e6fff; font-size: 36px; letter-spacing: 8px;'>{otpCode}</h1>
                    <p>This code will expire in 5 minutes.</p>
                    <p style='color: #666;'>If you didn't request this, please ignore this email.</p>
                </div>
            "
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            try
            {
                // Connect to Gmail SMTP server
                await client.ConnectAsync(
                    emailSettings["SmtpServer"],
                    int.Parse(emailSettings["SmtpPort"]!),
                    SecureSocketOptions.StartTls  // Use StartTls for port 587
                );

                // Authenticate
                await client.AuthenticateAsync(
                    emailSettings["Username"],
                    emailSettings["Password"]
                );

                // Send email
                await client.SendAsync(message);

                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending email: {ex.Message}", ex);
            }
        }

        public async Task SendYouthOtpEmailAsync(string toEmail, string otpCode, string youthName)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                emailSettings["SenderName"],
                emailSettings["SenderEmail"]
            ));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Youth Portal Access - Verification Code";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; border-radius: 10px 10px 0 0;'>
                <h1 style='color: white; margin: 0; font-size: 24px;'>eSKHub Bacacay</h1>
                <p style='color: white; margin: 5px 0 0 0; opacity: 0.9;'>Youth Portal Access</p>
            </div>
            <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;'>
                <h2 style='color: #333; margin-top: 0;'>Hello, {youthName}!</h2>
                <p style='color: #666; font-size: 16px;'>Enter this verification code to access your youth portal:</p>
                <div style='background: white; padding: 20px; border-radius: 8px; text-align: center; margin: 20px 0;'>
                    <h1 style='color: #7e6fff; font-size: 48px; letter-spacing: 12px; margin: 0;'>{otpCode}</h1>
                </div>
                <p style='color: #999; font-size: 14px; margin: 20px 0 0 0;'>
                    This code will expire in 5 minutes.<br>
                    If you didn't request this code, please ignore this email.
                </p>
            </div>
            <div style='text-align: center; padding: 20px; color: #999; font-size: 12px;'>
                <p>© 2025 eSKHub Bacacay. All rights reserved.</p>
            </div>
        </div>
        "
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync(
                    emailSettings["SmtpServer"],
                    int.Parse(emailSettings["SmtpPort"]!),
                    SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(
                    emailSettings["Username"],
                    emailSettings["Password"]
                );

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending email: {ex.Message}", ex);
            }
        }
        public async Task SendFeedbackOtpEmailAsync(string toEmail, string otpCode, string youthName)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                emailSettings["SenderName"],
                emailSettings["SenderEmail"]
            ));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Feedback Verification - Code: " + otpCode;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
            <div style='background: linear-gradient(135deg, #2b6cb0 0%, #4299e1 100%); padding: 30px; border-radius: 10px 10px 0 0;'>
                <h1 style='color: white; margin: 0; font-size: 24px;'>eSKHub Bacacay</h1>
                <p style='color: white; margin: 5px 0 0 0; opacity: 0.9;'>Feedback Submission Verification</p>
            </div>
            <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;'>
                <h2 style='color: #333; margin-top: 0;'>Hello, {youthName}!</h2>
                <p style='color: #666; font-size: 16px;'>You are about to submit feedback on eSKHub. Please use this verification code to continue:</p>
                <div style='background: white; padding: 20px; border-radius: 8px; text-align: center; margin: 20px 0; border: 1px solid #e2e8f0;'>
                    <h1 style='color: #2b6cb0; font-size: 48px; letter-spacing: 12px; margin: 0;'>{otpCode}</h1>
                </div>
                <p style='color: #999; font-size: 14px; margin: 20px 0 0 0;'>
                    This code will expire in 5 minutes.<br>
                    If you didn't request this verification, please ignore this email.
                </p>
            </div>
            <div style='text-align: center; padding: 20px; color: #999; font-size: 12px;'>
                <p>© {DateTime.Now.Year} eSKHub Bacacay. All rights reserved.</p>
            </div>
        </div>
        "
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync(
                    emailSettings["SmtpServer"],
                    int.Parse(emailSettings["SmtpPort"]!),
                    SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(
                    emailSettings["Username"],
                    emailSettings["Password"]
                );

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending email: {ex.Message}", ex);
            }
        }
    }
}