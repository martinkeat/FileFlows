using System.IO;
using FileFlows.FlowRunner.TemplateRenders;
using FileFlows.ServerShared.FileServices;

namespace FileFlowTests.Tests;

[TestClass]
public class ScribanTests
{
//     [TestMethod]
//     public void ScribanTest()
//     {
//         string text = @"
// File: {{Variables.file.Orig.FullName}}
// Extension: {{Variables.ext}}
// {{if Variables.file.Orig.Size > 100000 -}}
//     Size is greater than 100000.
//     Size is {{Variables.file.Orig.Size}}
// {{else -}}
//     Size is not greater than 100000.
//     Size is {{Variables.file.Orig.Size}}
// {{- end -}}
// ";
//         var tempFile = Path.GetTempFileName();
//         System.IO.File.WriteAllText(tempFile, string.Join("\n", Enumerable.Range(0, 10000).Select(x => Guid.NewGuid().ToString())));
//         var logger = new TestLogger();
//         var args = new NodeParameters(tempFile, logger, false, string.Empty, new LocalFileService());
//         args.InitFile(tempFile);
//         var renderer = new ScribanRenderer();
//         var rendered = renderer.Render(args, text);
//         var log = logger.ToString();
//     }
    
    [TestMethod]
    public void HandlebarTest()
    {
        string text = @"
File: {{Variables.file.Orig.FullName}}
Extension: {{Variables.ext}}
";
        var tempFile = Path.GetTempFileName();
        System.IO.File.WriteAllText(tempFile, string.Join("\n", Enumerable.Range(0, 10000).Select(x => Guid.NewGuid().ToString())));
        var logger = new TestLogger();
        var args = new NodeParameters(tempFile, logger, false, string.Empty, new LocalFileService());
        args.InitFile(tempFile);
        var renderer = new HandlebarsRenderer();
        var rendered = renderer.Render(args, text);
        var log = logger.ToString();
    }
}