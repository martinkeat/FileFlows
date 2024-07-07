using FileFlows.DataLayer.Reports.Charts;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FileFlows.Server.Charting;

public class LineChart : ImageChart 
{
    /// <summary>
    /// Generates an ImageSharp image based on MultilineChartData.
    /// </summary>
    /// <param name="chartData">The chart data.</param>
    /// <param name="customWidth">The width of the image.</param>
    /// <param name="customHeight">The height of the image.</param>
    /// <returns>The base64 encoded image tag.</returns>
    public string GenerateImage(MultilineChartData chartData, int? customWidth = null, int? customHeight = null)
    {
        var width = customWidth ?? EmailChartWidth;
        var height = customHeight ?? EmailChartHeight;
        // Initialize ImageSharp Image
        using var image = new Image<Rgba32>(width, height);

        // Define chart dimensions and positions
        int chartStartX = 50;
        int chartStartY = 50;
        int chartEndX = 20;
        int chartWidth = width - chartStartX - chartEndX;
        int chartHeight = height - chartStartY - 40; // Adjusted for x-axis label

        // Calculate maximum value from series data
        double maxValue = chartData.Series.SelectMany(s => s.Data).Max();

        // Draw chart elements using Mutate()
        image.Mutate(ctx =>
        {
            // Draw background
            ctx.Fill(Rgba32.ParseHex("#e4e4e4"), new Rectangle(chartStartX, chartStartY, chartWidth, chartHeight));

            // Draw y-axis labels and grid lines
            DrawYAxis(ctx, chartData, chartStartX, chartStartY, chartWidth, chartHeight, maxValue);

            // Draw x-axis labels and ticks
            DrawXAxis(ctx, chartData, chartStartX, chartStartY, chartWidth, chartHeight);

            // Draw series lines
            DrawSeriesLines(ctx, chartData, chartStartX, chartStartY, chartWidth, chartHeight, maxValue);
        });

        return ImageToBase64ImgTag(image);
    }

    private void DrawYAxis(IImageProcessingContext ctx, MultilineChartData chartData, int chartStartX, int chartStartY, int chartWidth, int chartHeight, double maxValue)
    {
        const int yAxisLabelFrequency = 4;
        const int yAxisLabelOffset = 10;
        const string lineColor = "#afafaf";
        const string foregroundColor = "#000";

        int yAxisHeight = chartHeight;
        for (int i = 0; i <= yAxisLabelFrequency; i++)
        {
            double value = (maxValue / yAxisLabelFrequency) * i;
            int y = chartStartY + chartHeight - (int)((value / maxValue) * yAxisHeight);

            // Draw grid line
            ctx.DrawLine(Rgba32.ParseHex(lineColor), 1, new PointF(chartStartX, y), new PointF(chartStartX + chartWidth, y));

            // Format y-axis label
            object yValue = string.IsNullOrWhiteSpace(chartData.YAxisFormatter) ? (object)Convert.ToInt64(value) : (object)value;
            string yLabel = ChartFormatter.Format(yValue, chartData.YAxisFormatter, axis: true);

            // Draw y-axis label
            ctx.DrawText(yLabel, Font, Rgba32.ParseHex(foregroundColor), new PointF(chartStartX - yAxisLabelOffset, y - 5));
            
            // Draw y-axis tick
            ctx.DrawLine(Rgba32.ParseHex(lineColor), 1, new PointF(chartStartX - 5, y), new PointF(chartStartX, y));
        }

        // Draw y-axis main label if provided
        // if (!string.IsNullOrEmpty(chartData.YAxisLabel))
        // {
        //     ctx.DrawText(chartData.YAxisLabel, SystemFonts.CreateFont("Arial", 12), Rgba32.ParseHex(foregroundColor), new PointF(chartStartX - yAxisLabelOffset - 30, chartStartY + chartHeight / 2));
        // }
    }

    private void DrawXAxis(IImageProcessingContext ctx, MultilineChartData chartData, int chartStartX, int chartStartY, int chartWidth, int chartHeight)
    {
        int totalLines = chartData.Labels.Length;
        int maxLabels = 10;
        int step = Math.Max(totalLines / maxLabels, 1);
        int xAxisLabelOffset = 40;
        const string lineColor = "#afafaf";
        const string foregroundColor = "#000";

        // Calculate the total width needed by the lines and spacing
        double xStep = (double)chartWidth / (totalLines - 1);

        // Format for x-axis labels
        var minDateUtc = chartData.Labels.Min();
        var maxDateUtc = chartData.Labels.Max();
        var totalDays = (maxDateUtc - minDateUtc).Days;

        string xAxisFormat = "{0:MMM} '{0:yy}";
        if (totalDays <= 1)
            xAxisFormat = "{0:HH}:00";
        else if (totalDays <= 180)
            xAxisFormat = "{0:%d} {0:MMM}";

        // Draw x-axis labels and ticks
        for (int i = 0; i < totalLines; i += step)
        {
            string label = string.Format(xAxisFormat, chartData.Labels[i].ToLocalTime());
            int x = chartStartX + (int)(i * xStep);

            // Draw x-axis label
            ctx.DrawText(label, Font, Rgba32.ParseHex(foregroundColor), new PointF(x, chartStartY + chartHeight + xAxisLabelOffset));

            // Draw x-axis tick
            ctx.DrawLine(Rgba32.ParseHex(lineColor), 1, new PointF(x, chartStartY + chartHeight), new PointF(x, chartStartY + chartHeight + 5));
        }
    }

    private void DrawSeriesLines(IImageProcessingContext ctx, MultilineChartData chartData, int chartStartX, int chartStartY, int chartWidth, int chartHeight, double maxValue)
    {
        const int lineThickness = 2;

        // Calculate the total width needed by the lines and spacing
        int totalLines = chartData.Series.Max(x => x.Data.Length);
        double xStep = (double)chartWidth / (totalLines - 1);

        // Draw series lines
        for (int seriesIndex = 0; seriesIndex < chartData.Series.Length; seriesIndex++)
        {
            var series = chartData.Series[seriesIndex];
            string color = COLORS[seriesIndex % COLORS.Length]; // Cycling through COLORS array

            // Build polyline points
            List<PointF> points = new List<PointF>();
            for (int i = 0; i < series.Data.Length; i++)
            {
                float x = chartStartX + (float)(i * xStep);
                float y = chartStartY + chartHeight - (float)(series.Data[i] / maxValue * chartHeight);
                points.Add(new PointF(x, y));
            }
            
            // Draw polyline for the series
            for (int i = 1; i < points.Count; i++)
            {
                ctx.DrawLine(Rgba32.ParseHex(color), lineThickness, points[i - 1], points[i]);
            }

        }
    }
}