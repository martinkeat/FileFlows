using FileFlows.Node.Workers;
using FileFlows.Server.Workers;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using FileFlows.Server.Middleware;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace FileFlows.Server;

/// <summary>
/// Web Server for the FileFlows Server
/// </summary>
public class WebServer
{
    private static WebApplication app;
    public static int Port { get; private set; }
    
    /// <summary>
    /// Stops the server
    /// </summary>
    public static async Task Stop()
    {
        if (app == null)
            return;
        await app.StopAsync();
    }

    /// <summary>
    /// Starts the server
    /// </summary>
    /// <param name="args">command line arguments</param>
    public static void Start(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        string protocol = "http";
        Port = 5000;
#if (DEBUG)
        Port = 6868;
#endif
        
        var url = args?.Where(x => x?.StartsWith("--urls=") == true)?.FirstOrDefault();
        if(string.IsNullOrEmpty(url) == false)
        {
            var portMatch = Regex.Match(url, @"(?<=(:))[\d]+");
            if (portMatch.Success)
                Port = int.Parse(portMatch.Value);
            if (url.StartsWith("https"))
                protocol = "https";
        }
        if (int.TryParse(Environment.GetEnvironmentVariable("Port"), out int port) && port is > 0 and <= 65535)
            Port = port;
        if (Environment.GetEnvironmentVariable("HTTPS") == "1")
            protocol = "https";

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddSignalR();
        builder.Services.AddResponseCompression();
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault | System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });
        builder.Services.AddMvc();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "FileFlows", Version = "v1" });

            var filePath = Path.Combine(System.AppContext.BaseDirectory, "FileFlows.Server.xml");
            if (File.Exists(filePath))
                c.IncludeXmlComments(filePath);
            else
            {
                filePath = Path.Combine(System.AppContext.BaseDirectory, "FileFlows.xml");
                if (File.Exists(filePath))
                    c.IncludeXmlComments(filePath);
            }
        });

        // if (File.Exists("/https/certificate.crt"))
        // {
        //     Console.WriteLine("Using certificate: /https/certificate.crt");
        //     Logger.Instance.ILog("Using certificate: /https/certificate.crt");
        //     builder.WebHost.ConfigureKestrel((context, options) =>
        //     {
        //         var cert = File.ReadAllText("/https/certificate.crt");
        //         var key = File.ReadAllText("/https/privatekey.key");
        //         var x509 = X509Certificate2.CreateFromPem(cert, key);
        //         X509Certificate2 miCertificado2 = new X509Certificate2(x509.Export(X509ContentType.Pkcs12));
        //
        //         x509.Dispose();
        //
        //         options.ListenAnyIP(5001, listenOptions =>
        //         {
        //             listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        //             listenOptions.UseHttps(miCertificado2);
        //         });
        //     });
        // }

        app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.IndexStream = () => typeof(WebServer).Assembly.GetManifestResourceStream("FileFlows.Server.Resources.SwaggerIndex.html");

            c.RoutePrefix = "api/help";
            c.DocumentTitle = "FileFlows API";
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileFlows API");
            c.InjectStylesheet("/css/swagger.min.css");
        });

        app.UseDefaultFiles();

        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        provider.Mappings[".br"] = "text/plain";

        //var wwwroot = Path.Combine(DirectoryHelper.BaseDirectory, "Server", "wwwroot");
        app.UseStaticFiles(new StaticFileOptions
        {
            //FileProvider = new PhysicalFileProvider(wwwroot),
            ContentTypeProvider = provider,
            OnPrepareResponse = x =>
            {
                if (x?.File?.PhysicalPath?.ToLower()?.Contains("_framework") == true)
                    return;
                if (x?.File?.PhysicalPath?.ToLower()?.Contains("_content") == true)
                    return;
                x?.Context?.Response?.Headers?.Append("Cache-Control", "no-cache");
            }
        });

        app.UseMiddleware<ExceptionMiddleware>();
        app.UseMiddleware<LoggingMiddleware>();
        // this is an experiment, may reuse it one day
        //app.UseMiddleware<UiMiddleware>();
        app.UseRouting();


        Globals.IsDevelopment = app.Environment.IsDevelopment();

        if (Globals.IsDevelopment)
            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("*"));

        app.MapControllerRoute(
             name: "default",
             pattern: "{controller=Home}/{action=Index}/{id?}");

        // this will allow refreshing from a SPA url to load the index.html file
        app.MapControllerRoute(
            name: "Spa",
            pattern: "{*url}",
            defaults: new { controller = "Home", action = "Index" }
        );


        Services.InitServices.Init();

#if(DEBUG)
        //Helpers.DbHelper.CleanDatabase().Wait();
#endif

        // do this so the settings object is loaded
        var settings = new Controllers.SettingsController().Get().Result;


        // need to scan for plugins before initing the translater as that depends on the plugins directory
        Helpers.PluginScanner.Scan();

        Helpers.TranslaterHelper.InitTranslater();

        ServerShared.Services.Service.ServiceBaseUrl = $"{protocol}://localhost:{Port}";
        // update the client with the proper ServiceBaseUrl
        Shared.Helpers.HttpHelper.Client = Shared.Helpers.HttpHelper.GetDefaultHttpHelper(ServerShared.Services.Service.ServiceBaseUrl);


        LibraryWorker.ResetProcessing(internalOnly: true);
        WorkerManager.StartWorkers(
            new LicenseValidatorWorker(),
            new SystemMonitor(),
            new LibraryWorker(),
            new LogFileCleaner(),
            new DbLogPruner(),
            new FlowWorker(string.Empty, isServer: true),
            new ConfigCleaner(),
            new PluginUpdaterWorker(),
            new LibraryFileLogPruner(),
            new LogConverter(),
            new TelemetryReporter(),
            new ServerUpdater(),
            new TempFileCleaner(string.Empty),
            new FlowRunnerMonitor(),
            new ObjectReferenceUpdater(),
            new FileFlowsTasksWorker(),
            new RepositoryUpdaterWorker(),
            new LibraryFileServiceUpdater()
        );

        app.MapHub<Hubs.FlowHub>("/flow");

        app.UseResponseCompression();

        // this will run the asp.net app and wait until it is killed
        Console.WriteLine("Running FileFlows Server");

        app.Run($"{protocol}://0.0.0.0:{Port}/");                
        
        Console.WriteLine("Finished running FileFlows Server");

        WorkerManager.StopWorkers();
    }
}
