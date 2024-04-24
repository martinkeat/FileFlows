using System.Text.RegularExpressions;

namespace FileFlows.Server.Cli;

/// <summary>
/// The command line interface parser
/// </summary>
internal static class CommandLine
{
    /// <summary>
    /// Attempts to process the command line
    /// </summary>
    /// <param name="args">the command line args</param>
    /// <returns>true if command line was processed and the application should now exit</returns>
    public static bool Process(string[] args)
    {
        if (args?.Any() != true)
            return false;

        var logger = new CliLogger();
        try
        {
            if (string.IsNullOrWhiteSpace(args[0]) || HelpArgument(args[0]))
            {
                PrintHelp(logger);
                return true;
            }
            string cmdSwitch = args[0].ToLower();
            var types = GetCommandTypes();
            foreach (var type in types)
            {
                var command = Activator.CreateInstance(type) as Command;
                var commandSwitch = command?.Switch?.Replace("-", "")?.ToLowerInvariant();
                var cliSwitch = cmdSwitch.TrimStart('-', '/').Replace("-", "").ToLowerInvariant();
                if(commandSwitch == cliSwitch)
                {
                    if (SecondArgumentHelp(args))
                    {
                        command.PrintHelp(logger);
                        return true;
                    }
                    try
                    {
                        command.ParseArguments(logger, args.Skip(1).ToArray());
                    }
                    catch(Exception exArgs)
                    {
                        command.PrintHelp(logger);
                        logger.ILog("");
                        logger.ILog(exArgs.Message);
                        return true;
                    }
                    if(command.Run(logger))
                        return true;
                }
            }
        }
        catch (Exception ex)
        {
            logger.ELog(ex.Message);
            return true;
        }

        return false;
    }

    private static bool SecondArgumentHelp(string[] args)
    {
        if (args.Length < 2)
            return false;
        return HelpArgument(args[1]);

    }
    private static bool HelpArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return false;

        return Regex.IsMatch(arg.ToLower(), @"^([\-]{0,2}|[/]?)(help|\?)$");
    }

    private static Type[] GetCommandTypes()
    {
        return typeof(Command).Assembly.GetTypes().Where(x => 
            x.BaseType == typeof(Command) && x.IsAbstract == false).ToArray();
    }

    internal static void PrintHelp(Plugin.ILogger logger)
    {
        logger.ILog("FileFlows v" + Globals.Version);
        logger.ILog("");
        List<(string, string)> args = new ();
        foreach(var type in GetCommandTypes())
        {
            try
            {
                if(Activator.CreateInstance(type) is Command { PrintToConsole: true } instance)
                    args.Add(("--" + instance.Switch, instance.Description));
            }
            catch (Exception) { }
        }
        args.Sort();
        var maxLength = args.Max(x => x.Item1.Length);
        foreach (var arg in args)
            logger.ILog(arg.Item1.PadRight(maxLength) + " : " + arg.Item2);
        
        logger.ILog("");
    }
}