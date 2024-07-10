namespace FileFlows.Charting;

/// <summary>
/// Bar chart
/// </summary>
public class BarChart : XYChart
{
    /// <summary>
    /// Generates a bar chart.
    /// </summary>
    /// <param name="chartData">The chart data.</param>
    /// <param name="customWidth">The width of the image.</param>
    /// <param name="customHeight">The height of the image.</param>
    /// <returns>The base64 encoded image tag.</returns>
    public string GenerateImage(BarChartData chartData, int? customWidth = null, int? customHeight = null)
    {
        // Initialize ImageSharp Image
        var (width, height) = GetImageSize(customWidth, customHeight);
        using var image = new Image<Rgba32>(width, height);

        // Calculate maximum value from chart data
        double maxValue = chartData.Data.Values.Max();

        // Draw chart elements using Mutate()
        image.Mutate(ctx =>
        {
            // Define chart dimensions and positions
            int chartStartX = CalculateXAxisStart(ctx, maxValue, chartData.YAxisFormatter);
            int chartStartY = 10;
            int chartEndX = 20;
            int chartWidth = width - chartStartX - chartEndX;
            int chartHeight = height - chartStartY - 40; // Adjusted for x-axis label
            
            // Draw background
            ctx.Fill(ChartAreaBackgroundColor, new Rectangle(chartStartX, chartStartY, chartWidth, chartHeight));

            // Draw y-axis labels and grid lines
            DrawYAxis(ctx, chartStartX, chartStartY, chartWidth, chartHeight, maxValue, chartData.YAxisFormatter);

            // Draw x-axis labels and ticks
            DrawXAxis(ctx, chartData, chartStartX, chartStartY, chartWidth, chartHeight);

            // Draw bars
            DrawBars(ctx, chartData, chartStartX, chartStartY, chartWidth, chartHeight, maxValue);
        });

        return ImageToBase64ImgTag(image);
    }

    private void DrawXAxis(IImageProcessingContext ctx, BarChartData chartData, int chartStartX, int chartStartY, int chartWidth, int chartHeight)
    {
        int totalBars = chartData.Data.Count;
        int maxLabels = 10;
        int step = Math.Max(totalBars / maxLabels, 1);
        int xAxisLabelOffset = 10;

        // Calculate the total width needed by the bars and spacing
        double barWidth = (double)chartWidth / totalBars;

        // Draw x-axis labels and ticks
        int i = 0;
        foreach (var kvp in chartData.Data)
        {
            if (i % step == 0)
            {
                string label = kvp.Key;
                int x = chartStartX + (int)(i * barWidth) + (int)(barWidth / 2);

                // Draw x-axis tick
                ctx.DrawLine(LineColor, 1 * Scale, new PointF(x, chartStartY + chartHeight), new PointF(x, chartStartY + chartHeight + 5));
                
                // Measure the size of the text
                var textOptions = new TextOptions(Font);
                var textSize = TextMeasurer.MeasureSize(label, textOptions);

                // Calculate the position for centered text
                var labelPosition = new PointF(x - (textSize.Width / 2), chartStartY + chartHeight + xAxisLabelOffset);
                
                // Draw x-axis label
                ctx.DrawText(label, Font, TextBrush, TextPen, labelPosition);
            }
            i++;
        }
    }
    
    /// <summary>
    /// Draws the bars for the bar chart.
    /// </summary>
    /// <param name="ctx">The image processing context.</param>
    /// <param name="chartData">The bar chart data.</param>
    /// <param name="chartStartX">The starting x-coordinate of the chart.</param>
    /// <param name="chartStartY">The starting y-coordinate of the chart.</param>
    /// <param name="chartWidth">The width of the chart area.</param>
    /// <param name="chartHeight">The height of the chart area.</param>
    /// <param name="maxValue">The maximum value in the chart data.</param>
    private void DrawBars(IImageProcessingContext ctx, BarChartData chartData, int chartStartX, int chartStartY, int chartWidth, int chartHeight, double maxValue)
    {
        int totalBars = chartData.Data.Count;
        double padding = 10 * Scale; // Adjust padding as needed
        double availableWidth = chartWidth - (padding * (totalBars + 1)); // Adjust total width for padding
        double barWidth = availableWidth / totalBars;

        var color = Rgba32.ParseHex(COLORS[0]);
        int i = 0;
        foreach (var kvp in chartData.Data)
        {
            double value = kvp.Value;

            float x = chartStartX + (float)(i * (barWidth + padding)) + (float)padding;
            float y = chartStartY + chartHeight - (float)((value / maxValue) * chartHeight);
            float barHeight = (float)((value / maxValue) * chartHeight);

            // Draw bar
            ctx.Fill(color, new RectangleF(x, y, (float)barWidth, barHeight));

            i++;
        }
    }
}
