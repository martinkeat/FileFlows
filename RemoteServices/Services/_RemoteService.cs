namespace FileFlows.RemoteServices;

/// <summary>
/// Remote service
/// </summary>
public abstract class RemoteService 
{
    private static string _ServiceBaseUrl;
    /// <summary>
    /// Gets or sets the Base URL of the FileFlows server
    /// </summary>
    public static string ServiceBaseUrl 
    { 
        get => _ServiceBaseUrl;
        set
        {
            if(value == null)
            {
                _ServiceBaseUrl = string.Empty;
                return;
            }
            if(value.EndsWith("/"))
                _ServiceBaseUrl = value.Substring(0, value.Length - 1); 
            else
                _ServiceBaseUrl = value;
        }
    }

    /// <summary>
    /// Gets or sets the API Token
    /// </summary>
    public static string ApiToken { get; set; }

    /// <summary>
    /// Gets or sets the Node UID whose making these requests
    /// </summary>
    public static Guid NodeUid { get; set; }

}