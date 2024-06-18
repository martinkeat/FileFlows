using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace FileFlows.Server.Helpers;

/// <summary>
/// Helper for template json
/// </summary>
public class TemplateHelper
{
    /// <summary>
    /// Replace the paths for windows path if running on windows
    /// </summary>
    /// <param name="json">the template json</param>
    /// <returns>the updated json</returns>
    public static string ReplaceWindowsPathIfWindows(string json)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
            return json;
        string userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        json = Regex.Replace(json, "/media/([^/\"]+)/([^\"]*)",
            System.Web.HttpUtility.JavaScriptStringEncode(userDir + "\\$1\\$2"));
        json = Regex.Replace(json, "/media/([^\"]*)", System.Web.HttpUtility.JavaScriptStringEncode(userDir + "\\$1"));
        json = json.Replace("\"/media\"", "\"" + System.Web.HttpUtility.JavaScriptStringEncode(userDir) + "\"");
        return json;
    }
    
    /// <summary>
    /// Replaces all instances of "/media/A" where A is any letter with a custom string followed by the capitalized version of the letter.
    /// </summary>
    /// <param name="template">The template containing instances of "/media/A".</param>
    /// <returns>A string with all instances of "/media/A" replaced by the custom string and capitalized letter.</returns>
    public static string ReplaceMediaWithHomeDirectory(string template)
    {
      // Define the regex pattern to match /media/ followed by any letter
      string pattern = @"/media/([a-zA-Z])";

      string home = DirectoryHelper.GetUsersHomeDirectory() + Path.DirectorySeparatorChar;

      home = home.Replace("\\", "\\\\");

      // Define the replacement function using MatchEvaluator
      string result = Regex.Replace(template, pattern, match =>
      {
        // Get the letter from the match
        char letter = match.Groups[1].Value[0];
        // Convert the letter to uppercase
        char upperLetter = char.ToUpper(letter);
        // Return the replacement string
        return home + upperLetter;
      });

      return result;
    }

    /// <summary>
    /// Finds and extracts items inside a JSON array based on a specified property name.
    /// </summary>
    /// <param name="json">The JSON string containing the data.</param>
    /// <param name="property">The name of the property whose array items to extract.</param>
    /// <returns>A list of extracted JSON array items as strings.</returns>
    static List<string> FindJsonArrayItems(string json, string property)
    {
      List<string> matches = new List<string>();

      string pattern = $@"""{property}"":\s*\[(?:[^[\]]|\[(?<open>)|\](?<-open>))*\]";
      MatchCollection propertyMatches = Regex.Matches(json, pattern);

      foreach (Match propertyMatch in propertyMatches)
      {
        string matchValue = propertyMatch.Value;

        var startIndex = matchValue.IndexOf("[") + 1;
        var endIndex = matchValue.LastIndexOf("]");

        string jsonArrayContent = matchValue.Substring(startIndex, endIndex - startIndex);

        //string arrayPattern = @"\{(?:[^{}]|(?<open>{)|(?<-open>}))*(?(open)(?!))\}";
        string arrayPattern = @"(?:\{(?:[^{}]|(?<open>{)|(?<-open>}))*(?(open)(?!))\})|""[^""]*""";

        foreach (Match itemMatch in Regex.Matches(jsonArrayContent, arrayPattern))
        {
          string itemValue = itemMatch.Value;
          if (itemValue.StartsWith("\"") && itemValue.EndsWith("\""))
            itemValue = JsonSerializer.Deserialize<string>(itemValue) ?? string.Empty;
          matches.Add(itemValue);
        }
      }

      return matches;
    }

    /// <summary>
    /// Replaces the special "OutputPath" flow field/element with common values
    /// </summary>
    /// <param name="json">the template json</param>
    /// <returns>the updated json</returns>
    public static string ReplaceOutputPathVariable(string json)
    {
        json = ReplaceOutputPathField(json, out List<string> deleteIfEmpty);
        json = ReplaceOutputPathParts(json, deleteIfEmpty);
        return json;
    }
    
    /// <summary>
    /// Replaces the OutputPath flow field with the actual flow fields to use
    /// </summary>
    /// <param name="json">the template json</param>
    /// <param name="deleteIfEmpty">regular expressions to add to delete if empty</param>
    /// <returns>the updated json</returns>
    private static string ReplaceOutputPathField(string json,out List<string> deleteIfEmpty)
    {
        deleteIfEmpty = new();
        var fields = FindJsonArrayItems(json, "Fields");
        foreach (string field in fields)
        {
            if (field.Replace(" ", string.Empty).ToLowerInvariant().Contains("\"type\":5") == false)
                continue;

            deleteIfEmpty = FindJsonArrayItems(field, "Options");

            json = json.Replace(field, @"        
        {
          ""Name"": ""Output_File"",
          ""Type"": 4,
          ""Description"": ""Where the newly converted file should be saved to"",
          ""Options"": [
            ""Replace Original"",
            ""Save to Folder""
          ],
          ""Required"": false,
          ""IfName"": """",
          ""IfValue"": """",
          ""IfNot"": false
        },
        {
          ""Name"": ""Destination_Folder"",
          ""Type"": 3,
          ""Description"": ""The folder where the converted file will be saved to"",
          ""FlowElementField"": ""bc419bf3-a2e6-4a3b-927f-a577f0eda010.DestinationPath"",
          ""Required"": false,
          ""IfName"": ""Output_File"",
          ""IfValue"": ""Save to Folder"",
          ""IfNot"": false
        },
        {
          ""Name"": ""Delete_Original"",
          ""Type"": 2,
          ""Description"": ""If the original file should be deleted or not"",
          ""FlowElementField"": ""bc419bf3-a2e6-4a3b-927f-a577f0eda010.DeleteOriginal"",
          ""Required"": false,
          ""IfName"": ""Output_File"",
          ""IfValue"": ""Save to Folder"",
          ""IfNot"": false
        }" + (deleteIfEmpty?.Any() != true ? string.Empty : @",
        {
          ""Name"": ""Delete_Directory_If_Empty"",
          ""Type"": 2,
          ""Description"": ""If the source directory should be deleted if it is now empty"",
          ""Required"": false,
          ""IfName"": ""Delete_Original"",
          ""IfValue"": ""true"",
          ""IfNot"": false
}"));
        }

        for (int i = deleteIfEmpty.Count - 1; i >= 0; i--)
        {
          if (deleteIfEmpty[i] == "audio")
          {
            deleteIfEmpty.RemoveAt(i);
            deleteIfEmpty.AddRange("mp3,wav,ogg,aac,wma,flac,alac,m4a,m4p".Split(',').Select(x => $"\\.{x}$"));
          }
          else if (deleteIfEmpty[i] == "video")
          {
            deleteIfEmpty.RemoveAt(i);
            deleteIfEmpty.AddRange("avi,ts,mp4,mkv,mpe,mpg,mpeg,mov,mpv,flv,wmv,webm,avchd,h265,h264".Split(',').Select(x => $"\\.{x}$"));
          }
          else if (deleteIfEmpty[i] == "image")
          {
            deleteIfEmpty.RemoveAt(i);
            deleteIfEmpty.AddRange("jpeg,jpe,jpg,tiff,tif,gif,png,webp,tga,pbm,bmp".Split(',').Select(x => $"\\.{x}$"));
          }
          else if (deleteIfEmpty[i] == "comic")
          {
            deleteIfEmpty.RemoveAt(i);
            deleteIfEmpty.AddRange("cbz,cbr,cb7,pdf,gz,bz2".Split(',').Select(x => $"\\.{x}$"));
          }
        }

        return json;
    }
    /// <summary>
    /// Replaces the OutputPath flow parts with the actual flow parts to use
    /// </summary>
    /// <param name="json">the template json</param>
    /// <param name="deleteIfEmpty">regular expressions to add to delete if empty</param>
    /// <returns>the updated json</returns>
    private static string ReplaceOutputPathParts(string json, List<string> deleteIfEmpty)
    {
        var parts = FindJsonArrayItems(json, "Parts");
        foreach (string part in parts)
        {
            if (part.Contains("FileFlows.BasicNodes.Templating.OutputPath") == false)
                continue;

            using JsonDocument doc = JsonDocument.Parse(part);
                JsonElement root = doc.RootElement;

            if (root.TryGetProperty("xPos", out JsonElement xPosElement) == false ||
                root.TryGetProperty("yPos", out JsonElement yPosElement) == false || 
                root.TryGetProperty("Uid", out JsonElement uidElement) == false)
                continue;
            
            int xPos = xPosElement.GetInt32();
            int yPos = yPosElement.GetInt32();
            var uid = uidElement.GetString();
            
            json = json.Replace(part, $@"        
{{
  ""Uid"": ""{uid}"",
  ""Name"": ""Output_File"",
  ""FlowElementUid"": ""FileFlows.BasicNodes.Conditions.IfString"",
  ""xPos"": {xPos},
  ""yPos"": {yPos},
  ""Icon"": ""fas fa-question"",
  ""Label"": """",
  ""Inputs"": 1,
  ""Outputs"": 2,
  ""OutputConnections"": [
    {{
      ""Input"": 1,
      ""Output"": 1,
      ""InputNode"": ""98e82bb5-b852-46c5-bd89-5c700b387b8f""
    }},
    {{
      ""Input"": 1,
      ""Output"": 2,
      ""InputNode"": ""bc419bf3-a2e6-4a3b-927f-a577f0eda010""
    }}
  ],
  ""Type"": 3,
  ""Model"": {{
    ""Outputs"": 2,
    ""Options"": [
      ""Replace Original"",
      ""Save to Folder""
    ],
    ""Variable"": ""Output_File""
  }}
}},
{{
  ""Uid"": ""98e82bb5-b852-46c5-bd89-5c700b387b8f"",
  ""Name"": """",
  ""FlowElementUid"": ""FileFlows.BasicNodes.File.ReplaceOriginal"",
  ""xPos"": {xPos},
  ""yPos"": {yPos},
  ""Icon"": ""fas fa-file"",
  ""Label"": """",
  ""Inputs"": 1,
  ""Outputs"": 1,
  ""Type"": 2,
  ""Model"": {{
    ""PreserverOriginalDates"": false
  }}
}},
{{
  ""Uid"": ""bc419bf3-a2e6-4a3b-927f-a577f0eda010"",
  ""Name"": """",
  ""FlowElementUid"": ""FileFlows.BasicNodes.File.MoveFile"",
  ""xPos"": {xPos},
  ""yPos"": {yPos},
  ""Icon"": ""fas fa-file-export"",
  ""Label"": """",
  ""Inputs"": 1,
  ""Outputs"": 2,
  ""OutputConnections"": [
    {{
      ""Input"": 1,
      ""Output"": 1,
      ""InputNode"": ""00e14acb-e0e2-4fc2-9167-724648c48352""
    }}
  ],
  ""Type"": 2,
  ""Model"": {{
    ""DestinationPath"": ""/media"",
    ""DestinationFile"": null,
    ""MoveFolder"": false,
    ""DeleteOriginal"": false,
    ""AdditionalFiles"": [],
    ""AdditionalFilesFromOriginal"": false,
    ""PreserverOriginalDates"": false
  }}
}}" 
  + (deleteIfEmpty?.Any() != true ? string.Empty : $@",
{{
  ""Uid"": ""00e14acb-e0e2-4fc2-9167-724648c48352"",
  ""Name"": ""Delete Directory If Empty"",
  ""FlowElementUid"": ""FileFlows.BasicNodes.Conditions.IfBoolean"",
  ""xPos"": {xPos},
  ""yPos"": {yPos + 220},
  ""Icon"": ""fas fa-question"",
  ""Label"": """",
  ""Inputs"": 1,
  ""Outputs"": 2,
  ""OutputConnections"": [
    {{
      ""Input"": 1,
      ""Output"": 1,
      ""InputNode"": ""a67d7cb9-108f-433e-99f5-55377d99cfd8""
    }}
  ],
  ""Type"": 3,
  ""Model"": {{
    ""Variable"": ""Delete_Directory_If_Empty""
  }}
}},
{{
  ""Uid"": ""a67d7cb9-108f-433e-99f5-55377d99cfd8"",
  ""Name"": ""Delete Source Folder If Empty"",
  ""FlowElementUid"": ""FileFlows.BasicNodes.File.DeleteSourceDirectory"",
  ""xPos"": {xPos},
  ""yPos"": {yPos + 220},
  ""Icon"": ""far fa-trash-alt"",
  ""Label"": """",
  ""Inputs"": 1,
  ""Outputs"": 2,
  ""Type"": 2,
  ""Model"": {{
    ""IfEmpty"": true,
    ""IncludePatterns"": {System.Text.Json.JsonSerializer.Serialize(deleteIfEmpty)}
  }}
}}
"));
        }

        return json;
    }
}