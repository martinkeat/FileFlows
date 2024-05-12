using System.IO;
using FileFlows.FlowRunner.Helpers;

namespace FileFlowTests.Tests.RunnerTests.ImageHelperTests;

/// <summary>
/// Tests PDF fiels
/// </summary>
[TestClass]
public class PdfTests : TestBase
{
    /// <summary>
    /// Test that extract images from a PDF file
    /// </summary>
    [TestMethod]
    public void ExtractImages()
    {
        const string PDF = "/home/john/Comics/unprocessed/pdf-comic-3.pdf";
        var magick = new ImageMagickHelper("convert", "identify");
        var result = magick.ExtractPdfImages(PDF, "/home/john/Comics/test");
        if(result.Failed(out string error))
            Assert.Fail(error);
        Assert.IsTrue(result.Value);
    }
    /// <summary>
    /// Test that extract images from a PDF file
    /// </summary>
    [TestMethod]
    public void CreatePdf()
    {
        const string PDF = "/home/john/Comics/test/pdf.pdf";
        var magick = new ImageMagickHelper("convert", "identify");
        var images = Directory.GetFiles("/home/john/Comics/test", "*.png").OrderBy(x => x).ToArray();
        var result = magick.CreatePdfFromImages(PDF, images);
        if(result.Failed(out string error))
            Assert.Fail(error);
        Assert.IsTrue(result.Value);
    }
}