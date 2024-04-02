using FileFlows.Plugin;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;

namespace FileFlows.Server.Helpers;

/// <summary>
/// Emailer helper that sends emails
/// </summary>
class Emailer
{
    /// <summary>
    /// Sends an email containing a users license key
    /// </summary>
    /// <param name="toName">the name of whom to sent the license key to</param>
    /// <param name="toAddress">the email address of whom to sent the license key to</param>
    /// <param name="subject">the subject</param>
    /// <param name="body">the body of the email</param>
    /// <returns>The final free-form text response from the server.</returns>
    internal static async Task<Result<string>> Send(string toName, string toAddress, string subject, string body)
    {
        var settings = await ServiceLoader.Load<SettingsService>().Get();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.SmtpFrom?.EmptyAsNull() ?? "FileFlows", settings.SmtpFromAddress?.EmptyAsNull() ?? "no-reply@fileflows.local"));
        message.To.Add(new MailboxAddress(toName?.EmptyAsNull() ?? toAddress, toAddress));
        message.Subject = subject;

        message.Body = new TextPart("plain")
        {
            Text = body
        };
        
        try
        {
            using var client = new SmtpClient();

            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            await client.ConnectAsync(settings.SmtpServer, settings.SmtpPort, settings.SmtpSecurity switch
            {
                EmailSecurity.Auto => SecureSocketOptions.Auto,
                EmailSecurity.SSL => SecureSocketOptions.SslOnConnect,
                EmailSecurity.TLS => SecureSocketOptions.StartTls,
                _ => SecureSocketOptions.None   
            });

            if (string.IsNullOrWhiteSpace(settings.SmtpUser) == false)
            {
                // Note: only needed if the SMTP server requires authentication
                await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword);
            }

            string response = await client.SendAsync(message);
            await client.DisconnectAsync(true);
            return response;
        }
        catch(Exception ex)
        {
            return Result<string>.Fail(ex.Message);
        }
    }
}
