using System.Text;
using PhotinoNET;

namespace FileFlows.Server.Gui.Photino;

/// <summary>
/// Photino web view
/// </summary>
public class WebView
{
    /// <summary>
    /// Opens a web view at the given URL
    /// </summary>
    /// <param name="url">the url to the FileFlows UI</param>
    public static void Open(string url)
    {
        string siteUrl = url;
        string folderPrefix = "";
#if (DEBUG)
        folderPrefix = "../Client/";
        siteUrl = "http://localhost:5276/";
#endif
        url = url.TrimEnd('/') + "/api/status";

        var iconFile = folderPrefix + "wwwroot/icon" + (PhotinoWindow.IsWindowsPlatform ? ".ico" : ".png");


        // Creating a new PhotinoWindow instance with the fluent API
        var window = new PhotinoWindow()
            .SetTitle("FileFlows")
            // Resize to a percentage of the main monitor work area
            .SetUseOsDefaultSize(false)
            .SetSize(new System.Drawing.Size(1600, 1080))
            .Center()
            .SetChromeless(false)
            .SetIconFile(iconFile)
            .SetResizable(true)
            //.SetMaximized(true)
            // Center window in the middle of the screen
            // Users can resize windows by default.
            // Let's make this one fixed instead.
            .RegisterCustomSchemeHandler("app",
                (object sender, string scheme, string url, out string contentType) =>
                {
                    contentType = "text/javascript";
                    return new MemoryStream(Encoding.UTF8.GetBytes(@"
                        (() =>{
                            window.setTimeout(() => {
                                alert(`🎉 Dynamically inserted JavaScript.`);
                            }, 1000);
                        })();
                    "));
                })
            // Most event handlers can be registered after the
            // PhotinoWindow was instantiated by calling a registration 
            // method like the following RegisterWebMessageReceivedHandler.
            // This could be added in the PhotinoWindowOptions if preferred.
            .RegisterWebMessageReceivedHandler((object sender, string message) =>
            {
                var window = (PhotinoWindow)sender;

                // The message argument is coming in from sendMessage.
                // "window.external.sendMessage(message: string)"
                string response = $"Received message: \"{message}\"";

                // Send a message back the to JavaScript event handler.
                // "window.external.receiveMessage(callback: Function)"
                window.SendWebMessage(response);
            })
            //.Load("wwwroot/index.html"); // Can be used with relative path strings or "new URI()" instance to load a website.
            //.Load("http://localhost:5276/")
            .LoadRawString($@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Loading Page</title>
    <style>
        body {{
            --base-darkest-rgb:7, 7, 7;
            --base-darkest:rgb(7, 7, 7);
            background-color: var(--base-darkest);
            color: #fff;
            font-size:14px;
            font-family: Arial, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
        }}

        svg {{
            height:3rem;
        }}
        
        .version {{
            position:absolute;
            bottom:3rem;
        }}
    </style>
</head>
<body>

<div id=""loading"">
    <svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" version=""1.1"" width=""520"" height=""105"" viewBox=""0 0 315.15 63.64"" xml:space=""preserve"" fill=""#ff0090"">
<g transform=""matrix(0.86 0 0 0.97 36.11 32.98)"" >
	<path style=""stroke: none; stroke-width: 1; stroke-dasharray: none; stroke-linecap: butt; stroke-dashoffset: 0; stroke-linejoin: miter; stroke-miterlimit: 4; fill-rule: nonzero; opacity: 1;""  transform="" translate(-32, -31.95)"" d=""M 55.4 43.4 h -7.2 c -3.3 0 -6.2 2.2 -7.1 5.3 H 18.9 c -4.1 0 -7.5 -3.4 -7.5 -7.5 s 3.4 -7.5 7.5 -7.5 h 26.2 c 6.1 0 11 -4.9 11 -11 c 0 -6.1 -4.9 -11 -11 -11 H 23.2 v -0.5 c 0 -4.1 -3.3 -7.4 -7.4 -7.4 H 8.6 c -4.1 0 -7.4 3.3 -7.4 7.4 v 1.9 c 0 4.1 3.3 7.4 7.4 7.4 h 7.2 c 3.3 0 6.2 -2.2 7.1 -5.3 h 22.2 c 4.1 0 7.5 3.4 7.5 7.5 s -3.4 7.5 -7.5 7.5 H 18.9 c -6.1 0 -11 4.9 -11 11 s 4.9 11 11 11 h 21.9 v 0.5 c 0 4.1 3.3 7.4 7.4 7.4 h 7.2 c 4.1 0 7.4 -3.3 7.4 -7.4 v -1.9 C 62.8 46.7 59.4 43.4 55.4 43.4 z M 19.7 13.2 c 0 2.1 -1.7 3.9 -3.9 3.9 H 8.6 c -2.1 0 -3.9 -1.7 -3.9 -3.9 v -1.9 c 0 -2.1 1.7 -3.9 3.9 -3.9 h 7.2 c 2.1 0 3.9 1.7 3.9 3.9 V 13.2 z M 59.3 52.7 c 0 2.1 -1.7 3.9 -3.9 3.9 h -7.2 c -2.1 0 -3.9 -1.7 -3.9 -3.9 v -1.9 c 0 -2.1 1.7 -3.9 3.9 -3.9 h 7.2 c 2.1 0 3.9 1.7 3.9 3.9 V 52.7 z"" stroke-linecap=""round"" />
</g>
<g transform=""matrix(1 0 0 1 503 27.79)"" style=""""  >
		<text xml:space=""preserve"" font-family=""'Open Sans', sans-serif"" font-size=""18"" font-style=""normal"" font-weight=""normal"" style=""stroke: none; stroke-width: 1; stroke-dasharray: none; stroke-linecap: butt; stroke-dashoffset: 0; stroke-linejoin: miter; stroke-miterlimit: 4; fill-rule: nonzero; opacity: 1; white-space: pre;"" ><tspan x=""-432.45"" y=""0.31"" style=""font-size: 1px; font-style: italic; font-weight: bold; "">FileFlows</tspan></text>
</g>
<g transform=""matrix(1 0 0 1 283.05 35.03)"" style=""""  >
		<text xml:space=""preserve"" font-family=""'Open Sans', sans-serif"" font-size=""50"" font-style=""normal"" font-weight=""normal"" style=""stroke: none; stroke-width: 1; stroke-dasharray: none; stroke-linecap: butt; stroke-dashoffset: 0; stroke-linejoin: miter; stroke-miterlimit: 4; fill-rule: nonzero; opacity: 1; white-space: pre;"" ><tspan x=""-214.5"" y=""16.02"" style=""font-size: 51px; font-style: italic; font-weight: bold; "">FileFlows</tspan></text>
</g>
</svg>
</div>

<div class=""version"">
    {Globals.Version}
</div>
</body>
</html>
");
        
        Task.Run(async () =>
        {

            while (WebServer.Started == false && WebServer.StartError == null)
                await Task.Delay(250);

            if (WebServer.StartError != null)
            {
                window.LoadRawString($@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Error Page</title>
<style>
    body {{
        margin: 0;
        padding: 0;
        background-color: black;
        color: white;
        font-family: Arial, sans-serif;
        display: flex;
        justify-content: center;
        align-items: center;
        height: 100vh;
    }}

    .error-message {{
        text-align: center;
    }}
</style>
</head>
<body>
    <div class=""error-message"">
        <h1>Error!</h1>
        <p>{WebServer.StartError}</p>
    </div>
</body>
</html>
");
            }
            else
            {
                await Task.Delay(3000);
                window.Load(siteUrl);
            }
        });


        window.WaitForClose(); // Starts the application event loop
    }
}