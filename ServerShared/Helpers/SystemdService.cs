using System.Diagnostics;
using System.IO;

namespace FileFlows.ServerShared.Helpers;


/// <summary>
/// Helper to install FileFlows node as a systemd service
/// </summary>
public class SystemdService
{

    /// <summary>
    /// Gets the location where to save the users systemd service file
    /// </summary>
    /// <returns></returns>
    private static string GetSystemdServiceFolder()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config", "systemd", "user");
    
    /// <summary>
    /// Installs the service
    /// </summary>
    /// <param name="baseDirectory">the base directory for the FileFiles install, DirectoryHelper.BaseDirectory</param>
    /// <param name="isNode">if installing node or server</param>
    public static void Install(string baseDirectory, bool isNode)
    {
        string bashScript = CreateEntryPoint(baseDirectory, isNode);
        if (SaveServiceFile(baseDirectory, isNode, bashScript) == false)
            return;
        RunService(isNode);
        Console.WriteLine("Run the following to check the status of the service: ");
        string name = isNode ? "fileflows-node" : "fileflows";
        Console.WriteLine($"systemctl --user status {name}.service ");
        Console.WriteLine();
    }

    /// <summary>
    /// Uninstall the service
    /// </summary>
    /// <param name="isNode">if uninstalling node or server</param>
    public static void Uninstall(bool isNode)
    {
        string name = isNode ? "fileflows-node" : "fileflows";
        
        Process.Start("systemctl", "--user stop " + name);
        Process.Start("systemctl", "--user disable " + name);

        string serviceFile = Path.Combine(GetSystemdServiceFolder(), name + ".service");
        if(File.Exists(serviceFile))
            File.Delete(serviceFile);
        
        Process.Start("systemctl", "--user daemon-reload");
        Process.Start("systemctl", "--user reset-failed");
    }

    /// <summary>
    /// Runs the service
    /// </summary>
    /// <param name="isNode">if installing node or server</param>
    private static void RunService(bool isNode)
    {
        string name = isNode ? "fileflows-node" : "fileflows";
        Process.Start("systemctl", $"--user enable {name}.service");
        Process.Start("systemctl", "--user daemon-reload");
        Process.Start("systemctl", $"--user start {name}.service");
    }

    private static string GetDotnetLocation()
    {
        string whereIsDotnet = ExecuteToString("whereis", "dotnet");
        // dotnet: /usr/share/dotnet /home/john/.dotnet/dotnet
        if (whereIsDotnet?.StartsWith("dotnet:") == false)
            return "dotnet";
        whereIsDotnet = whereIsDotnet![7..].Trim();
        int spaceIndex = whereIsDotnet.IndexOf(' ');
        if (spaceIndex > 0)
            whereIsDotnet = whereIsDotnet.Substring(0, spaceIndex);
        if (string.IsNullOrWhiteSpace(whereIsDotnet))
            return "dotnet";
        if (Directory.Exists(whereIsDotnet)) // check if it is a directory
            return whereIsDotnet + "/dotnet";
        return whereIsDotnet;
    }

    /// <summary>
    /// Executes a command and returns the standard output
    /// </summary>
    /// <param name="cmd">the command to execute</param>
    /// <param name="args">the parameters for the command</param>
    /// <returns>the command output</returns>
    private static string ExecuteToString(string cmd, string args)
    {
        try
        {
            using Process process = new Process();
            process.StartInfo.FileName = cmd;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            //* Read the output (or the error)
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Create an entry point file that will be used by systemd to start FileFlows
    /// </summary>
    /// <param name="baseDirectory">the base directory for the FileFiles install, DirectoryHelper.BaseDirectory</param>
    /// <param name="isNode">if installing node or server</param>
    /// <returns>the entry point bash script</returns>
    private static string CreateEntryPoint(string baseDirectory, bool isNode)
    {
        string dotnet = GetDotnetLocation();
        string updatePath = isNode ? "NodeUpdate" : "Update";
        string updateScript = isNode ? "node-upgrade.sh" : "server-upgrade.sh";
        string fullUS = updatePath + "/" + updateScript;
        string appName = isNode ? "FileFlows Node" : "FileFlows";
        string appPath = isNode ? "Node" : "Server";
        string dll = isNode ? "FileFlows.Node.dll" : "FileFlows.Server.dll";
        string shScript = $@"#!/usr/bin/env bash

if test -f ""{fullUS}""; then
    echo ""Upgrade found""
    chmod +x {fullUS}
    cd {updatePath}
    echo ""bash {updateScript} systemd""
    bash {updateScript} systemd
fi

printf ""Launching {appName}\n""
cd {appPath}
exec {dotnet} {dll} --no-gui --systemd-service
";
        string entryPoint = Path.Combine(baseDirectory, "fileflows" + (isNode ? "-node" : "") + "-systemd-entrypoint.sh");
        File.WriteAllText(entryPoint, shScript);
        FileHelper.MakeExecutable(entryPoint);
        return entryPoint;
    }

    /// <summary>
    /// Saves the service configuration file
    /// </summary>
    /// <param name="baseDirectory">the base directory for the FileFiles install, DirectoryHelper.BaseDirectory</param>
    /// <param name="isNode">if installing node or server</param>
    /// <param name="bashScript">the bash script used to run the service</param>
    /// <returns>if it was successful or not</returns>
    private static bool SaveServiceFile(string baseDirectory, bool isNode, string bashScript)
    {
        string contents = $@"[Unit]
Description={(isNode ? "FileFlows Node" :"FileFlows")}

[Service]
WorkingDirectory={baseDirectory}
ExecStart={bashScript} 
SyslogIdentifier={(isNode ? "FileFlows Node" :"FileFlows")}
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target";

        string file = Path.Combine(GetSystemdServiceFolder(), $"fileflows{(isNode ? "-node" : "")}.service");
        var fileInfo = new FileInfo(file);
        if (fileInfo.Directory?.Exists == false)
        {
            try
            {
                fileInfo.Directory.Create();
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to create directory: " + fileInfo.Directory.FullName);
                return false;
            }
        }

        try
        {
            File.WriteAllText(file, contents);
            Console.WriteLine("Created service file: " + file);
            return true;
        }
        catch (Exception)
        {
            Console.WriteLine("Failed to create file: " + file);
            return false;
        }
    }
}