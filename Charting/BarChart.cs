namespace FileFlows.Charting;

/// <summary>
/// Bar chart
/// </summary>
public class BarChart : ImageChart
{
    const int yAxisLabelFrequency = 4;
    const int yAxisLabelOffset = 10;
    
    /// <summary>
    /// Generates a bar chart.
    /// </summary>
    /// <param name="chartData">The chart data.</param>
    /// <param name="customWidth">The width of the image.</param>
    /// <param name="customHeight">The height of the image.</param>
    /// <returns>The base64 encoded image tag.</returns>
    public string GenerateImage(BarChartData chartData, int? customWidth = null, int? customHeight = null)
    {
        var width = customWidth ?? EmailChartWidth;
        var height = customHeight ?? EmailChartHeight;
        width *= 2;
        height *= 2;
        
        // Initialize ImageSharp Image
        using var image = new Image<Rgba32>(width, height);


        // Calculate maximum value from chart data
        double maxValue = chartData.Data.Values.Max();

        // Draw chart elements using Mutate()
        image.Mutate(ctx =>
        {
            // Define chart dimensions and positions
            int chartStartX = CalculateXAxisStart(ctx, chartData, maxValue);
            int chartStartY = 10;
            int chartEndX = 20;
            int chartWidth = width - chartStartX - chartEndX;
            int chartHeight = height - chartStartY - 40; // Adjusted for x-axis label
            
            // Draw background
            ctx.Fill(Rgba32.ParseHex("#e4e4e4"), new Rectangle(chartStartX, chartStartY, chartWidth, chartHeight));

            // Draw y-axis labels and grid lines
            DrawYAxis(ctx, chartData, chartStartX, chartStartY, chartWidth, chartHeight, maxValue);

            // Draw x-axis labels and ticks
            DrawXAxis(ctx, chartData, chartStartX, chartStartY, chartWidth, chartHeight);

            // Draw bars
            DrawBars(ctx, chartData, chartStartX, chartStartY, chartWidth, chartHeight, maxValue);
        });

        return ImageToBase64ImgTag(image);
    }


    /// <summary>
    /// Calculates the width needed for the y-axis labels and where the x-axis should start
    /// </summary>
    /// <param name="ctx">the image context</param>
    /// <param name="chartData">the chart data</param>
    /// <param name="maxValue">the maximum value</param>
    /// <returns>the X start position</returns>
    private int CalculateXAxisStart(IImageProcessingContext ctx, BarChartData chartData, double maxValue)
    {
        float width = 0;
        for (int i = 0; i <= yAxisLabelFrequency; i++)
        {
            double value = (maxValue / yAxisLabelFrequency) * i;

            // Format y-axis label
            object yValue = string.IsNullOrWhiteSpace(chartData.YAxisFormatter)
                ? (object)Convert.ToInt64(value)
                : (object)value;
            string yLabel = ChartFormatter.Format(yValue, chartData.YAxisFormatter, axis: true);
            
            // Measure the size of the text
            var textOptions = new TextOptions(Font);
            var textSize = TextMeasurer.MeasureSize(yLabel, textOptions);

            width = Math.Max(width, textSize.Width);
        }

        return (int)width + 10;
    }
    
    private void DrawYAxis(IImageProcessingContext ctx, BarChartData chartData, int chartStartX, int chartStartY, int chartWidth, int chartHeight, double maxValue)
    {
        int yAxisHeight = chartHeight;

        for (int i = 0; i <= yAxisLabelFrequency; i++)
        {
            double value = (maxValue / yAxisLabelFrequency) * i;
            int y = chartStartY + chartHeight - (int)((value / maxValue) * yAxisHeight);

            // Draw grid line
            ctx.DrawLine(LineColor, 1 * Scale, new PointF(chartStartX, y), new PointF(chartStartX + chartWidth, y));

            if (i == 0)
                continue; // don't draw the first y-axis label it overlaps the x tick label

            // Draw y-axis tick
            ctx.DrawLine(LineColor, 1 * Scale, new PointF(chartStartX - 5, y), new PointF(chartStartX, y));
            
            // Format y-axis label
            object yValue = string.IsNullOrWhiteSpace(chartData.YAxisFormatter) ? (object)Convert.ToInt64(value) : (object)value;
            string yLabel = ChartFormatter.Format(yValue, chartData.YAxisFormatter, axis: true);

            // Measure the size of the text
            var textOptions = new TextOptions(Font);
            var textSize = TextMeasurer.MeasureSize(yLabel, textOptions);

            // Calculate the position for right-aligned text
            var labelPosition = new PointF(chartStartX - yAxisLabelOffset - textSize.Width, y - textSize.Height / 2 - 2);
            
            // Draw y-axis label
            ctx.DrawText(yLabel, Font, TextBrush, TextPen, labelPosition);
        }
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

    private void DrawBars(IImageProcessingContext ctx, BarChartData chartData, int chartStartX, int chartStartY, int chartWidth, int chartHeight, double maxValue)
    {
        int totalBars = chartData.Data.Count;
        double barWidth = (double)chartWidth / totalBars;

        int i = 0;
        foreach (var kvp in chartData.Data)
        {
            string label = kvp.Key;
            double value = kvp.Value;

            float x = chartStartX + (float)(i * barWidth);
            float y = chartStartY + chartHeight - (float)((value / maxValue) * chartHeight);
            float barHeight = (float)((value / maxValue) * chartHeight);

            // Draw bar
            ctx.Fill(Rgba32.ParseHex(COLORS[i % COLORS.Length]), new RectangleF(x, y, (float)barWidth - 2, barHeight));

            i++;
        }
    }
}
