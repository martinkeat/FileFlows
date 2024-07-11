using System.Web;
using Humanizer;

namespace FileFlows.DataLayer.Reports.Helpers;

/// <summary>
/// Report summary box for small single value information
/// </summary>
public class ReportSummaryBox
{
    /// <summary>
    /// Generates a report summary box
    /// </summary>
    /// <param name="title">the title</param>
    /// <param name="value">the value to display</param>
    /// <param name="icon">the icon to show</param>
    /// <param name="color">the color</param>
    /// <param name="emailing">if this is being emailed</param>
    /// <returns>the HTML of the report summary box</returns>
    public static string Generate(string title, string value, IconType icon, BoxColor color, bool emailing)
    {
        if (emailing == false)
            return $"<div class=\"report-summary-box {color.ToString().ToLowerInvariant()}\">" +
                   $"<span class=\"icon\">{GetIcon(icon)}</span>" +
                   $"<span class=\"title\">{HttpUtility.HtmlEncode(title)}</span>" +
                   $"<span class=\"value\">{HttpUtility.HtmlEncode(value)}</span>" +
                   "</div>";
        return $@"
<table style=""width:100%;"">
    <tr>
        <td class=""rsb-icon"" rowspan=""2"" style=""width:60px"">{GetEmailIcon(icon, color)}</td>
        <td class=""rsb-title"" style=""{ReportBuilder.EmailTitleStyling}"">{HttpUtility.HtmlEncode(title)}</td>
    </tr>
    <tr>
        <td class=""rsb-value"" style=""font-size:22px"">{HttpUtility.HtmlEncode(value)}</td>
    </tr>
</table>";
    }

    public enum BoxColor
    {
        Info, 
        Success,
        Warning,
        Error
    }
    public enum IconType
    {
        ArrowAltCircleDown,
        ArrowAltCircleUp,
        BalanceScale,
        Clock,
        ClosedCaptioning,
        ExclamationCircle,
        HardDrive,
        Hourglass,
        HourglassHalf,
        HourglassStart,
        HourglassEnd,
        File,
        Folder,
        Video,
        VolumeUp
    }

    /// <summary>
    /// Gets the icon
    /// </summary>
    /// <param name="icon">the icon</param>
    /// <returns>the HTML for the icon</returns>
    public static string GetIcon(IconType icon)
    {
        switch (icon)
        {
            case IconType.ArrowAltCircleDown:
                return "<i class=\"far fa-arrow-alt-circle-down\"></i>";
            case IconType.ArrowAltCircleUp:
                return "<i class=\"far fa-arrow-alt-circle-up\"></i>";
            case IconType.BalanceScale:
                return "<i class=\"fas fa-balance-scale\"></i>";
            case IconType.Clock:
                return "<i class=\"far fa-clock\"></i>";
            case IconType.ClosedCaptioning:
                return "<i class=\"far fa-closed-captioning\"></i>";
            case IconType.ExclamationCircle:
                return "<i class=\"fas fa-exclamation-circle\"></i>";
            case IconType.File:
                return "<i class=\"far fa-file\"></i>";
            case IconType.Folder:
                return "<i class=\"far fa-folder\"></i>";
            case IconType.HardDrive:
                return "<i class=\"far fa-hdd\"></i>";
            case IconType.Hourglass:
                return "<i class=\"far fa-hourglass\"></i>";
            case IconType.HourglassEnd:
                return "<i class=\"fas fa-hourglass-end\"></i>";
            case IconType.HourglassHalf:
                return "<i class=\"fas fa-hourglass-half\"></i>";
            case IconType.HourglassStart:
                return "<i class=\"fas fa-hourglass-start\"></i>";
            case IconType.Video:
                return "<i class=\"fas fa-video\"></i>";
            case IconType.VolumeUp:
                return "<i class=\"fas fa-volume-up\"></i>";
        }

        return string.Empty;
    }

#if (!DEBUG)
    /// <summary>
    /// The base directory of the application
    /// </summary>
    private static string? _BaseDirectory; 
#endif

    /// <summary>
    /// Gets the email icon
    /// </summary>
    /// <param name="icon">the icon</param>
    /// <param name="color">the color of the icon</param>
    /// <returns>the HTML for the icon</returns>
    public static string GetEmailIcon(IconType icon, BoxColor color)
    {
#if (DEBUG)
        var dir = "wwwroot/report-icons";
#else
        string dir;
        if (_BaseDirectory == null)
        {
            var dllDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(dllDir) == false)
            {
                _BaseDirectory = new DirectoryInfo(dllDir).Parent?.FullName ?? string.Empty;
                dir = Path.Combine(_BaseDirectory, "Server/wwwroot/report-icons");
            }
            else
            {
                dir = "Server/wwwroot/report-icons";
            }
        }
        else
        {
            dir =  Path.Combine(_BaseDirectory, "Server/wwwroot/report-icons");
        }
#endif
        string filePath = Path.Combine(dir, icon.ToString().Kebaberize() + "-" + color.ToString().ToLowerInvariant() + ".png");
        if (File.Exists(filePath) == false)
            return string.Empty;
        
        // Read file content as byte array
        byte[] fileBytes;
        try
        {
            fileBytes = File.ReadAllBytes(filePath);
        }
        catch (IOException)
        {
            // Handle any file IO exceptions here
            return string.Empty;
        }

        // Convert byte array to base64 string
        string base64String = Convert.ToBase64String(fileBytes);

        // Format as HTML <img> tag with base64 source
        string imgTag = $"<img style=\"width:48px;height:48px;padding-top:4px;\" src=\"data:image/png;base64,{base64String}\" />";

        return imgTag;
    }
}