using System.Net;

namespace FileFlows.RemoteServices;

/// <summary>
/// Email Service
/// </summary>
public class EmailService: RemoteService
{
    /// <summary>
    /// Sends an email to the provided recipients
    /// </summary>
    /// <param name="to">a list of email addresses</param>
    /// <param name="subject">the subject of the email</param>
    /// <param name="body">the plain text body of the email</param>
    /// <returns>true if successfully sent, otherwise false</returns>
    public async Task<Result<bool>> Send(string[] to, string subject, string body)
    {
        try
        {
            var result = await HttpHelper.Post($"{ServiceBaseUrl}/remote/email/send", new
            {
                To = to,
                Subject = subject,
                Body = body
            });
            if (result.StatusCode == HttpStatusCode.OK)
                return true;
            return Result<bool>.Fail(result.Body);
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed to send email: " + ex.Message);
            return Result<bool>.Fail(ex.Message);
        }
    }
}