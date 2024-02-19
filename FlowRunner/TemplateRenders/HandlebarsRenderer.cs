// using System.Text.RegularExpressions;
// using FileFlows.Plugin;
// using HandlebarsDotNet;
//
// namespace FileFlows.FlowRunner.TemplateRenders;
//
// /// <summary>
// /// Renderer for handle bars
// /// </summary>
// public class HandlebarsRenderer: ITemplateRenderer
// {
//     /// <inheritdoc />
//     public string Render(NodeParameters args, string text)
//     {
//         try
//         {
//             string tcode = text;
//             foreach (string k in args.Variables.Keys.OrderByDescending(x => x.Length))
//             {
//                 // replace Variables.Key or Variables?.Key?.Subkey etc to just the variable
//                 // so Variables.file?.Orig.Name, will be replaced to Variables["file.Orig.Name"] 
//                 // since its just a dictionary key value 
//                 string keyRegex = @"Variables(\?)?\." + k.Replace(".", @"(\?)?\.");
//
//                 string replacement = "Variables[\"" + k + "\"]";
//                 if (k.StartsWith("file.") || k.StartsWith("folder."))
//                 {
//                     // FF-301: special case, these are readonly, need to make these easier to use
//                     if (Regex.IsMatch(k, @"\.(Create|Modified)$"))
//                         continue; // dates
//                     if (Regex.IsMatch(k, @"\.(Year|Day|Month|Size)$"))
//                     {
//                         // replacement += " | cast";
//                     }
//                     else
//                     {
//                         // replacement += ".toString()";
//                     }
//                 }
//
//                 // object? value = Variables[k];
//                 // if (value is JsonElement jElement)
//                 // {
//                 //     if (jElement.ValueKind == JsonValueKind.String)
//                 //         value = jElement.GetString();
//                 //     if (jElement.ValueKind == JsonValueKind.Number)
//                 //         value = jElement.GetInt64();
//                 // }
//
//                 tcode = Regex.Replace(tcode, keyRegex, replacement);
//             }
//
//
//             var template = Handlebars.Compile(text);
//             var rendered = template(new { args.Variables });
//             return rendered;
//         }
//         catch (Exception ex)
//         {
//             args.Logger?.ELog("Failed rendering template: " + ex.Message);
//             return text;
//         }
//     }
// }