using System.Text;
using System.Text.RegularExpressions;

namespace FileFlows.ScriptExecution;

/// <summary>
/// Parses a script code block into a ScriptModel
/// </summary>
public class ScriptParser
{
    Regex rgxParameter = new Regex(@"(?<=(@param[\s]+))\{([^\}]+)\}[\s]+([\w]+)[\s]+(.*?)$");
    Regex rgxOutput = new Regex(@"(?<=(@output[\s]+))(.*?)$");
    
    /// <summary>
    /// Parses the code of a script and returns a ScriptModel
    /// </summary>
    /// <param name="name">the name of the script</param>
    /// <param name="code">the script to parse</param>
    /// <returns>a parsed model</returns>
    public (bool Success, ScriptModel? Model, string Error) Parse(string name, string code)
    {
        if (string.IsNullOrEmpty(code))
            return (false, null, "No script found");
        var rgxComments = new Regex(@"\/\*(\*)?(.*?)\*\/", RegexOptions.Singleline);
        var matchComments = rgxComments.Match(code.Trim());
        if (matchComments.Success == false)
            return (false, null, "Failed to locate comment section.  A script must start with a comment block describing the script.");
        var comments = matchComments.Value.Trim()[1..^1];
        code = code.Replace(matchComments.Value, string.Empty).Trim();
        // remove the start * 
        comments = string.Join("\n", comments.Replace("\r\n", "\n").Split('\n')
            .Select(x => Regex.Replace(x, @"^[\s]*[\*]+[\s]*", ""))).Trim();

        ScriptModel model = new()
        {
            Name = name,
            Code = code,
            Outputs = new (),
            Parameters = new()
        };
        var atIndex = comments.IndexOf('@');
        if (atIndex < 0)
            return (false, null, "No comment parameters found");
        
        model.Description = comments[..atIndex].Trim();
        
        comments = comments[atIndex..];

        foreach (var line in comments.Split('\n'))
        {
            if (ParseArgument(model, line))
                continue;
            if (ParseOutput(model, line))
                continue;
            if (line.StartsWith('@') == false)
            {
                if (string.IsNullOrWhiteSpace(model.Description))
                    model.Description = line;
                else
                    model.Description += "\n" + line;
            }
            else if (line.StartsWith("@name "))
                model.Name = line["@name ".Length..].Trim();
            else if (line.StartsWith("@uid ") && Guid.TryParse(line[5..].Trim(), out var uid))
                model.Uid = uid;
            else if (line.StartsWith("@revision ") && int.TryParse(line["@revision ".Length..].Trim(), out var revision))
                model.Revision = revision;
            else if (line.StartsWith("@description "))
                model.Description = line["@description ".Length..].Trim();
            else if (line.StartsWith("@author "))
                model.Author = line["@author ".Length..].Trim();
            else if (line.StartsWith("@minimumversion ", StringComparison.InvariantCultureIgnoreCase) &&
                     Version.TryParse(line["@minimumVersion ".Length..].Trim(), out var version))
                model.MinimumVersion = version;
            else if (line.StartsWith("@outputs") && int.TryParse(line[9..].Trim(), out var outputs))
            {
                for (int i = 1; i <= outputs; i++)
                {
                    model.Outputs.Add(new ()
                    {
                        Index = i,
                        Description = "Output " + i
                    });
                }
            }
        }

        return (true, model, string.Empty);
    }

    /// <summary>
    /// Parse a comment line and if argument will add it ot the model
    /// </summary>
    /// <param name="model">the ScriptModel to add the argument to</param>
    /// <param name="line">the comment line to parse</param>
    /// <returns>true if parsed as a argument</returns>
    /// <exception cref="Exception">throws exception if line is invalid</exception>
    private bool ParseArgument(ScriptModel model, string line)
    {
        var paramMatch = rgxParameter.Match(line);
        if (paramMatch.Success == false)
            return false;
        var param = new ScriptParameter();
        switch (paramMatch.Groups[2].Value.ToLower())
        {
            case "bool":
                param.Type = ScriptArgumentType.Bool;
                break;
            case "string":
                param.Type = ScriptArgumentType.String;
                break;
            case "int":
                param.Type = ScriptArgumentType.Int;
                break;
            default:
                throw new Exception("Invalid parameter type: " + paramMatch.Groups[2].Value);
        }

        try
        {
            param.Name = paramMatch.Groups[3].Value;
            param.Description = paramMatch.Groups[4].Value;
            model.Parameters.Add(param);
            return true;
        }
        catch (Exception)
        {
            throw new Exception("Invalid parameter: " + line);
        }
    }

    
    /// <summary>
    /// Parse a comment line and if an output will add it ot the model
    /// </summary>
    /// <param name="model">the ScriptModel to add the argument to</param>
    /// <param name="line">the comment line to parse</param>
    /// <returns>true if parsed as a output</returns>
    private bool ParseOutput(ScriptModel model, string line)
    {
        var match = rgxOutput.Match(line);
        if (match.Success == false)
            return false;
        ScriptOutput output = new ();
        output.Index = model.Outputs.Count + 1;
        output.Description = match.Value;
        model.Outputs.Add(output);
        return true;
    }

    /// <summary>
    /// Generates a comment block from a script
    /// </summary>
    /// <param name="script">the script</param>
    /// <returns>the comment block</returns>
    public string GenerateCommentBlock(ScriptModel script)
    {
        var header = new StringBuilder("/**");
        AddField("name", script.Name);
        AddField("description", script.Description);
        AddField("author", script.Author);
        AddField("revision", script.Revision?.ToString());
        AddField("minimumVersion", script.MinimumVersion?.ToString());
        if (script.Parameters?.Any() == true)
        {
            foreach (var parameter in script.Parameters)
            {
                header.AppendLine(" * @param " + (
                        parameter.Type switch
                        {
                            ScriptArgumentType.Bool => "{bool}",
                            ScriptArgumentType.Int => "{int}",
                            _ => "{string}",
                        }
                    ) + $" {parameter.Name} {parameter.Description}");
            }
        }

        if (script.Outputs?.Any() == true)
        {
            foreach (var output in script.Outputs.OrderBy(x => x.Index))
            {
                header.AppendLine($" * @output {(string.IsNullOrWhiteSpace(output.Description) ? $"Output {output.Index}" : output.Description)}");
            }
        }

        return header.ToString();

        void AddField(string name, string? value)
        {
            if (string.IsNullOrWhiteSpace(value) == false)
                header.AppendLine($" * @{name} {value}");
        }

    }
}