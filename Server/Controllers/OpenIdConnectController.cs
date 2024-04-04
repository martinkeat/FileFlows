using System.Web;
using System.Net.Http.Headers;
using FileFlows.Shared.Models;
using FileFlows.Server.Helpers;
using Microsoft.AspNetCore.Mvc;
using FileFlows.Server.Services;
using System.Text.Json.Serialization;
using HttpMethod = System.Net.Http.HttpMethod;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Controller for authenticating using OpenID Connect.
/// </summary>
[Route("oidc")]
public class OpenIDController : Controller
{
    private readonly Settings _settings;
    private static HttpClient _httpClient = new();

    /// <summary>
    /// Constructs a new instance of the OpenIDController
    /// </summary>
    /// <param name="settingsService">the settings service</param>
    public OpenIDController(SettingsService settingsService)
    {
        _settings = settingsService.Get().Result;
    }

    /// <summary>
    /// Action to initiate the authentication process.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Login(string returnUrl = "/")
    {
        // Check if the user is already authenticated
        if (User.Identity.IsAuthenticated)
            return LocalRedirect(returnUrl);

        var oidcConfig = await GetWellKnownConfig();
        if (oidcConfig == null)
            return ErrorPage();

        string idpAuthUrl = oidcConfig.AuthorizationEndpoint;
        string clientId = _settings.OidcClientId; // Replace with your client ID
        string redirectUri = Url.Action("Callback", "OpenID", null, Request.Scheme); // Callback URL

        string redirectUrl = $"{idpAuthUrl}?client_id={clientId}&redirect_uri={redirectUri}&response_type=code";

        redirectUrl += "&scope=openid+profile+email";
        // Redirect the user to the IdP for authentication
        return Redirect(redirectUrl);
    }

    /// <summary>
    /// Action to handle the authentication callback from the OpenID provider.
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback()
    {
        var oidcConfig = await GetWellKnownConfig();
        if (oidcConfig == null)
            return ErrorPage();

        try
        {
            // Retrieve the authentication response parameters from the query string
            string? code = Request.Query["code"];

            // Perform token validation
            var redirectUri = Url.Action("Callback", "OpenID", null, Request.Scheme); // Callback URL

            var tokenRequestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("client_id", _settings.OidcClientId),
                new KeyValuePair<string, string>("client_secret", _settings.OidcClientSecret)
            });

            var tokenEndpoint = oidcConfig.TokenEndpoint;
            HttpResponseMessage tokenResponse = await _httpClient.PostAsync(tokenEndpoint, tokenRequestContent);

            if (tokenResponse.IsSuccessStatusCode == false)
                return ErrorPage();

            var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();
            var tokenResult = JsonSerializer.Deserialize<OidcTokenResult>(tokenResponseContent);

            // Use the access token to fetch user information from the userinfo endpoint
            var userInfoEndpoint = oidcConfig.UserInfoEndpoint;

            // Create a new instance of HttpRequestMessage
            var request = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);

            // Set the Authorization header for this request
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

            // Send the request using the static HttpClient instance
            HttpResponseMessage userInfoResponse = await _httpClient.SendAsync(request);

            if (userInfoResponse.IsSuccessStatusCode == false)
                return ErrorPage();
            
            var userInfoResponseContent = await userInfoResponse.Content.ReadAsStringAsync();
            var oidcUser = JsonSerializer.Deserialize<OidcUserInfo>(userInfoResponseContent);

            var service = ServiceLoader.Load<UserService>();
            var user = await service.FindUser(oidcUser.Email?.EmptyAsNull() ?? oidcUser.Name);
            if (user == null)
                return ErrorPage();

            var jwt = SecurityHelper.CreateJwtToken(user, Request.GetActualIP());

            string url = "/auth-redirect.html?jwt=" + jwt;
            #if(DEBUG)
            url = "http://localhost:5276" + url;
            #endif
            return Redirect(url);
        }
        catch (Exception)
        {
            return ErrorPage();
        }
    }

    /// <summary>
    /// Error page
    /// </summary>
    /// <param name="message">the error message</param>
    /// <returns>the error page</returns>
    private IActionResult ErrorPage(string message = "User is unauthorized and cannot access this.")
    {
        var errorMessage = HttpUtility.HtmlEncode(message);
        var htmlContent = $@"
    <!DOCTYPE html>
    <html lang='en'>
    <head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>FileFlows</title>
    <style>
        body {{
            margin: 0;
            padding: 0;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            font-family: Arial, sans-serif;
            background: rgb(20,20,20);
            color:#fff;
        }}
        .error-container {{
            text-align: center;
            padding: 2rem;
            border: 0.1rem solid #ccc;
            border-radius: 1rem;
            width: 30rem;
        }}
        .error-message {{
            font-size: 1.2rem;
        }}
    </style>
    <script>
        // Replace the current URL with /login#error in the browser history
        history.replaceState(null, '', '/login#error');
    </script>
    </head>
    <body>
        <div class='error-container'>
            <div class='error-message'>{errorMessage}</div>
        </div>
    </body>
    </html>";
        return Content(htmlContent, "text/html");
    }

    /// <summary>
    /// Retrieves the OpenID Connect configuration.
    /// </summary>
    private async Task<OpenIDConnectConfiguration?> GetWellKnownConfig()
    {
        try
        {
            string url = _settings.OidcAuthority; // Replace with your IdP's authentication URL
            if (!url.StartsWith("http"))
                url = "https://" + url;
            url = url.TrimEnd('/');
            url += "/.well-known/openid-configuration";

            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<OpenIDConnectConfiguration>(json);
            }
        }
        catch (Exception)
        {
        }
        return null;
    }
}

/// <summary>
/// Represents the OpenID Connect configuration retrieved from the provider's discovery document.
/// </summary>
public class OpenIDConnectConfiguration
{
    /// <summary>
    /// Gets or sets the issuer URL of the OpenID Connect provider.
    /// </summary>
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; }

    /// <summary>
    /// Gets or sets the authorization endpoint URL of the OpenID Connect provider.
    /// </summary>
    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the token endpoint URL of the OpenID Connect provider.
    /// </summary>
    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the userinfo endpoint URL of the OpenID Connect provider.
    /// </summary>
    [JsonPropertyName("userinfo_endpoint")]
    public string UserInfoEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the JWKS (JSON Web Key Set) endpoint URL of the OpenID Connect provider.
    /// </summary>
    [JsonPropertyName("jwks_uri")]
    public string JwksUri { get; set; }

    /// <summary>
    /// Gets or sets the registration endpoint URL of the OpenID Connect provider.
    /// </summary>
    [JsonPropertyName("registration_endpoint")]
    public string RegistrationEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the supported scopes by the OpenID Connect provider.
    /// </summary>
    [JsonPropertyName("scopes_supported")]
    public string[] ScopesSupported { get; set; }

    /// <summary>
    /// Gets or sets the supported response types by the OpenID Connect provider.
    /// </summary>
    [JsonPropertyName("response_types_supported")]
    public string[] ResponseTypesSupported { get; set; }

    /// <summary>
    /// Gets or sets the supported grant types by the OpenID Connect provider.
    /// </summary>
    [JsonPropertyName("grant_types_supported")]
    public string[] GrantTypesSupported { get; set; }

    /// <summary>
    /// Gets or sets the supported response modes by the OpenID Connect provider.
    /// </summary>
    [JsonPropertyName("response_modes_supported")]
    public string[] ResponseModesSupported { get; set; }

    /// <summary>
    /// Gets or sets the supported token endpoint authentication methods by the OpenID Connect provider.
    /// </summary>
    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public string[] TokenEndpointAuthMethodsSupported { get; set; }

    /// <summary>
    /// Gets or sets the supported subject types by the OpenID Connect provider.
    /// </summary>
    [JsonPropertyName("subject_types_supported")]
    public string[] SubjectTypesSupported { get; set; }

    /// <summary>
    /// Gets or sets the supported ID token signing algorithms by the OpenID Connect provider.
    /// </summary>
    [JsonPropertyName("id_token_signing_alg_values_supported")]
    public string[] IdTokenSigningAlgValuesSupported { get; set; }
}

/// <summary>
/// Represents the token result returned by the OpenID Connect provider.
/// </summary>
public class OidcTokenResult
{
    /// <summary>
    /// Gets or sets the access token issued by the OpenID Connect provider.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the type of token issued (e.g., Bearer).
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    /// <summary>
    /// Gets or sets the ID token issued by the OpenID Connect provider.
    /// </summary>
    [JsonPropertyName("id_token")]
    public string IdToken { get; set; }

    /// <summary>
    /// Gets or sets the scope of the access token.
    /// </summary>
    [JsonPropertyName("scope")]
    public string Scope { get; set; }

    /// <summary>
    /// Gets or sets the lifetime of the access token in seconds.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

/// <summary>
/// Represents the user information returned by the OpenID Connect provider.
/// </summary>
public class OidcUserInfo
{
    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the user's given (first) name.
    /// </summary>
    [JsonPropertyName("given_name")]
    public string GivenName { get; set; }

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; }
}
