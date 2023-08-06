namespace FileFlows.Plugin;

using System.Text.RegularExpressions;

/// <summary>
/// A class to help the replacement of variables
/// </summary>
public class VariablesHelper
{
    /// <summary>
    /// Replaces variables in a given string
    /// </summary>
    /// <param name="input">the input string</param>
    /// <param name="variables">the variables used to replace</param>
    /// <param name="stripMissing">if missing variables shouild be removed</param>
    /// <param name="cleanSpecialCharacters">if special characters (eg directory path separator) should be replaced</param>
    /// <param name="encoder">Optional function to encode the variable values before replacing them</param>
    /// <returns>the string with the variables replaced</returns>
    public static string ReplaceVariables(string input, Dictionary<string, object> variables, bool stripMissing = false, bool cleanSpecialCharacters = false, Func<string, string> encoder = null)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        
        foreach(Match match in Regex.Matches(input,  @"{([a-zA-Z_][a-zA-Z0-9_.]*)(?:(!)|\|([^{}]*))?}"))
        {
            object value = match.Groups[1].Value;
            if (variables != null && variables.ContainsKey((string)value))
                value = variables[(string)value];
            
            var format = match.Groups
                .Cast<Group>()
                .Skip(2) // Skip the first group (group 0 is the entire match)
                .LastOrDefault(group => group.Success && !string.IsNullOrEmpty(group.Value))
                ?.Value ?? string.Empty;
            
            var formatter = Formatters.Formatter.GetFormatter(format);
            string strValue = formatter.Format(value, format);
            
            if(encoder != null)
                strValue = encoder(strValue);

            input = input.Replace(match.Value, strValue);
        }
    
        if (variables?.Any() == true)
        {
            foreach (string variable in variables.Keys)
            {
                string strValue = variables[variable]?.ToString() ?? "";
                if (cleanSpecialCharacters && variable.Contains(".") && variable.StartsWith("file.") == false && variable.StartsWith("folder.") == false)
                {
                    // we dont want to replace user variables they set themselves, eg they may have set "DestPath" or something in the Function node
                    // so we dont want to replace that, or any of the file/folder variables
                    // but other nodes generate variables based on metadata, and that metadata may contain a / or \ which would break a filename
                    strValue = strValue.Replace("/", "-");
                    strValue = strValue.Replace("\\", "-");
                }
                input = ReplaceVariable(input, variable, strValue);
            }
        }

        if (stripMissing)
            input = Regex.Replace(input, "{[a-zA-Z_][a-zA-Z0-9_]*}", string.Empty);

        return input;
    }

    /// <summary>
    /// Replaces a variable in the string
    /// </summary>
    /// <param name="input">the input string</param>
    /// <param name="variable">the variable to replace</param>
    /// <param name="value">the new value of the variable</param>
    /// <returns>the input string with the variable replaced</returns>
    private static string ReplaceVariable(string input, string variable, string value)
    {
        var result = input;
        if(Regex.IsMatch(result, @"{" + Regex.Escape(variable) + @"}"))
            result = Regex.Replace(result, @"{" + Regex.Escape(variable) + @"}", value, RegexOptions.IgnoreCase);
        if (Regex.IsMatch(result, @"{" + Regex.Escape(variable) + @"!}"))
            result = Regex.Replace(result, @"{" + Regex.Escape(variable) + @"!}", value.ToUpper(), RegexOptions.IgnoreCase);

        return result;
    }

}
