using System.Text;
using System.Text.Json;
using System.Web;

namespace FileFlows.DataLayer.Reports.Charts;

/// <summary>
/// Tree map chart
/// </summary>
public class TreeMap : Chart
{
    //private static readonly (int R, int G, int B, double A) BaseColor = (54, 169, 210, 0.85);
    private static readonly (int R, int G, int B, double A) BaseColor = (23, 162, 184, 0.85);
    private const double MaxDarkeningFactor = 0.4;

    /// <summary>
    /// Generates a tree map chart
    /// </summary>
    /// <param name="data">The data for the chart.</param>
    /// <param name="emailing">If the image generated should be an emailed</param>
    /// <returns>The chart content</returns>
    public static string Generate(TreeMapData data, bool emailing = false)
    {
        if (emailing == false)
            return $"<div class=\"chart\"><h2 class=\"title\">{HttpUtility.HtmlEncode(data.Title)}</h2>{JavaScript(data)}</div>";

        return @$"
<div>
    <span class=""chart-title"" style=""{ReportBuilder.EmailTitleStyling}"">{HttpUtility.HtmlEncode(data.Title)}</span>
    {FileFlows.DataLayer.Helpers.DelegateHelpers.GeneratePieChart?.Invoke(new ()
    {
        Data = data.Data,
        Title = data.Title
    })}
</div>";
    }

    /// <summary>
    /// Generates a tree map chart using javascript
    /// </summary>
    /// <param name="data">The data for the chart.</param>
    /// <returns>An HTML input to be parsed by javascript</returns>
    private static string JavaScript(TreeMapData data)
        => "<input type=\"hidden\" class=\"report-tree-map-data\" value=\"" + HttpUtility.HtmlEncode(JsonSerializer.Serialize(data.Data)) + "\" />";

    /// <summary>
    /// Generates an SVG tree map chart
    /// </summary>
    /// <param name="chartData">The data for the chart.</param>
    /// <returns>An SVG string representing the tree map chart.</returns>
    private static string Svg(TreeMapData chartData)
    {
        if (chartData.Data?.Any() != true)
            return string.Empty;

        double total = chartData.Data.Sum(item => (double)item.Value);
        var sortedData = chartData.Data.OrderByDescending(item => item.Value).ToList();
        double maxValue = sortedData.First().Value;

        var rectangles = CalculateRectangles(sortedData, total, 0, 0, EmailChartWidth, EmailChartHeight);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("<div class=\"chart tree-map\">");
        builder.AppendLine(
            $"<svg class=\"tree-map\" width=\"100%\" height=\"100%\" viewBox=\"0 0 {EmailChartWidth} {EmailChartHeight}\" preserveAspectRatio=\"none\" xmlns=\"http://www.w3.org/2000/svg\">");

        foreach (var rect in rectangles)
        {
            string label = rect.Label;
            double value = rect.Value;
            double percentage = (value / total) * 100;

            string color = CalculateColor(value, maxValue);
            string borderColor = CalculateColor(value, maxValue, border: true);
            string tooltip = $"{label}: {value} ({percentage:F2}%)";

            builder.AppendLine(
                $"<rect x=\"{rect.X}\" y=\"{rect.Y}\" width=\"{rect.Width}\" height=\"{rect.Height}\" fill=\"{color}\" data-title=\"{System.Net.WebUtility.HtmlEncode(tooltip)}\" class=\"tree-map-rect\"></rect>");
            
            builder.AppendLine(
                $"<rect x=\"{rect.X + 1}\" y=\"{rect.Y + 1}\" width=\"{rect.Width - 2}\" height=\"{rect.Height - 2}\" fill=\"none\" stroke=\"{borderColor}\" stroke-width=\"2\" />");

            builder.AppendLine(
                $"<text x=\"{rect.X + 5}\" y=\"{rect.Y + 25}\" font-size=\"14\" fill=\"#fff\">{label}</text>");
        }

        builder.AppendLine("</svg>");
        builder.AppendLine("</div>");
        return builder.ToString();
    }

    /// <summary>
    /// Calculates rectangles based on data distribution and dimensions.
    /// </summary>
    /// <param name="data">The data to be partitioned into rectangles.</param>
    /// <param name="total">The total value of the current partition.</param>
    /// <param name="x">The X-coordinate of the top-left corner of the initial partition.</param>
    /// <param name="y">The Y-coordinate of the top-left corner of the initial partition.</param>
    /// <param name="width">The width of the initial partition.</param>
    /// <param name="height">The height of the initial partition.</param>
    /// <returns>A list of TreeMapRectangle instances representing calculated rectangles.</returns>
    private static List<TreeMapRectangle> CalculateRectangles(List<KeyValuePair<string, int>> data, double total, double x, double y, double width, double height)
    {
        var rectangles = new List<TreeMapRectangle>();
        CalculateRectanglesRecursively(data, total, x, y, width, height, rectangles);
        return rectangles;
    }

    /// <summary>
    /// Calculates rectangles recursively based on data distribution and dimensions.
    /// </summary>
    /// <param name="data">The data to be partitioned into rectangles.</param>
    /// <param name="total">The total value of the current partition.</param>
    /// <param name="x">The X-coordinate of the top-left corner of the current partition.</param>
    /// <param name="y">The Y-coordinate of the top-left corner of the current partition.</param>
    /// <param name="width">The width of the current partition.</param>
    /// <param name="height">The height of the current partition.</param>
    /// <param name="rectangles">The list to store generated TreeMapRectangle instances.</param>
    private static void CalculateRectanglesRecursively(List<KeyValuePair<string, int>> data, double total, double x, double y, double width, double height, List<TreeMapRectangle> rectangles)
    {
        if (data.Count == 0)
            return;

        if (data.Count == 1)
        {
            rectangles.Add(new TreeMapRectangle
            {
                Label = data[0].Key,
                Value = data[0].Value,
                X = x,
                Y = y,
                Width = width,
                Height = height
            });
            return;
        }

        double halfTotal = total / 2;
        double runningTotal = 0;
        int splitIndex = 0;

        for (int i = 0; i < data.Count; i++)
        {
            runningTotal += data[i].Value;
            if (runningTotal >= halfTotal)
            {
                splitIndex = i;
                break;
            }
        }

        var firstPartition = data.Take(splitIndex + 1).ToList();
        var secondPartition = data.Skip(splitIndex + 1).ToList();

        double firstPartitionTotal = firstPartition.Sum(d => (double)d.Value);
        double secondPartitionTotal = secondPartition.Sum(d => (double)d.Value);

        if (width > height)
        {
            double firstWidth = width * (firstPartitionTotal / total);
            CalculateRectanglesRecursively(firstPartition, firstPartitionTotal, x, y, firstWidth, height, rectangles);
            CalculateRectanglesRecursively(secondPartition, secondPartitionTotal, x + firstWidth, y, width - firstWidth, height, rectangles);
        }
        else
        {
            double firstHeight = height * (firstPartitionTotal / total);
            CalculateRectanglesRecursively(firstPartition, firstPartitionTotal, x, y, width, firstHeight, rectangles);
            CalculateRectanglesRecursively(secondPartition, secondPartitionTotal, x, y + firstHeight, width, height - firstHeight, rectangles);
        }
    }
    /// <summary>
    /// Calculates the color based on the value relative to the maximum value, optionally adjusting for a border color.
    /// </summary>
    /// <param name="value">The current value of the rectangle.</param>
    /// <param name="maxValue">The maximum value among all rectangles.</param>
    /// <param name="border">Optional. Indicates whether the color is for the border.</param>
    /// <returns>The RGBA color string.</returns>
    private static string CalculateColor(double value, double maxValue, bool border = false)
    {
        double ratio = value / maxValue;
        double darkeningFactor = MaxDarkeningFactor * (1 - ratio);

        if (border)
        {
            darkeningFactor += 0.1; // Slightly darker for the border
        }

        int r = (int)(BaseColor.R * (1 - darkeningFactor));
        int g = (int)(BaseColor.G * (1 - darkeningFactor));
        int b = (int)(BaseColor.B * (1 - darkeningFactor));

        return $"rgba({r}, {g}, {b}, {BaseColor.A})";
    }
}
/// <summary>
/// Represents a rectangle in a tree map chart.
/// </summary>
public class TreeMapRectangle
{
    /// <summary>
    /// Gets or sets the label associated with the rectangle.
    /// </summary>
    public string Label { get; set; } = null!;

    /// <summary>
    /// Gets or sets the value associated with the rectangle.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets the X-coordinate of the top-left corner of the rectangle.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the Y-coordinate of the top-left corner of the rectangle.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the width of the rectangle.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the rectangle.
    /// </summary>
    public double Height { get; set; }
}


/// <summary>
/// Tree Map Data
/// </summary>
public class TreeMapData
{
    /// <summary>
    /// Gets or sets the title of the tree map
    /// </summary>
    public string Title { get; set; } = null!;
    /// <summary>
    /// The data
    /// </summary>
    public Dictionary<string, int> Data { get; set; } = null!;
}
