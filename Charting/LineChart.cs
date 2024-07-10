namespace FileFlows.Charting;

/// <summary>
/// Line chart
/// </summary>
public class LineChart : XYChart
{
    
    /// <summary>
    /// Generates an line chart.
    /// </summary>
    /// <param name="chartData">The chart data.</param>
    /// <param name="customWidth">The width of the image.</param>
    /// <param name="customHeight">The height of the image.</param>
    /// <returns>The base64 encoded image tag.</returns>
    public string GenerateImage(LineChartData chartData, int? customWidth = null, int? customHeight = null)
    {
        // Initialize ImageSharp Image
        var (width, height) = GetImageSize(customWidth, customHeight);
        using var image = new Image<Rgba32>(width, height);

        // Calculate maximum value from series data
        double maxValue = chartData.Series.SelectMany(s => s.Data).Max();

        // Draw chart elements using Mutate()
        image.Mutate(ctx =>
        {
            // Define chart dimensions and positions
            int chartStartX = CalculateXAxisStart(ctx, maxValue, chartData.YAxisFormatter);
            int chartStartY = 10;
            int chartEndX = 20;
            int chartWidth = width - chartStartX - chartEndX;
            int chartHeight = height - chartStartY - 40; // Adjusted for x-axis label
            
            chartHeight -= DrawLegend(ctx, chartData.Series.Select(x =>x.Name).ToArray(), width, height);
            
            // Draw background
            ctx.Fill(ChartAreaBackgroundColor, new Rectangle(chartStartX, chartStartY, chartWidth, chartHeight));

            // Draw y-axis labels and grid lines
            DrawYAxis(ctx, chartStartX, chartStartY, chartWidth, chartHeight, maxValue, chartData.YAxisFormatter);

            // Draw x-axis labels and ticks
            DrawXAxis(ctx, chartData, chartStartX, chartStartY, chartWidth, chartHeight);

            // Draw series lines
            DrawSeriesLines(ctx, chartData, chartStartX, chartStartY, chartWidth, chartHeight, maxValue);
        });

        return ImageToBase64ImgTag(image);
    }

    private void DrawXAxis(IImageProcessingContext ctx, LineChartData chartData, int chartStartX, int chartStartY, int chartWidth, int chartHeight)
    {
        int totalLines = chartData.Labels.Length;
        int maxLabels = 10;
        int step = Math.Max(totalLines / maxLabels, 1);
        int xAxisLabelOffset = 10;

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

            // Draw x-axis tick
            ctx.DrawLine(LineColor, 1 * Scale, new PointF(x, chartStartY + chartHeight), new PointF(x, chartStartY + chartHeight + 5));
            
            // Measure the size of the text
            var textOptions = new TextOptions(Font);
            var textSize = TextMeasurer.MeasureSize(label, textOptions);

            // Calculate the position for right-aligned text
            var labelPosition = new PointF(x - (textSize.Width / 2), chartStartY + chartHeight + xAxisLabelOffset);
            
            // Draw x-axis label
            ctx.DrawText(label, Font, TextBrush, TextPen, labelPosition);
        }
    }

    private void DrawSeriesLines(IImageProcessingContext ctx, LineChartData chartData, int chartStartX, int chartStartY, int chartWidth, int chartHeight, double maxValue)
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
                ctx.DrawLine(Rgba32.ParseHex(color), lineThickness * Scale, points[i - 1], points[i]);
            }
        }
    }

    /// <summary>
    /// Draws the legend
    /// </summary>
    /// <param name="ctx">The image processing context used for drawing</param>
    /// <param name="series">The chart series names</param>
    /// <param name="width">The width of the image</param>
    /// <param name="height">The height of the image</param>
    /// <returns>The height used up by the legend</returns>
    private int DrawLegend(IImageProcessingContext ctx, string[] series, int width, int height)
    {
        if (series.Length < 2)
            return 0; // No legend

        height -= 5; // just to give us some extra spacing 

        // Legend configuration
        float padding = 10 * Scale;
        float circleDiameter = 10 * Scale;
        float circleRadius = circleDiameter / 2;
        float verticalSpacing = 5 * Scale;
        float horizontalSpacing = 3 * Scale;
        float seriesSpacing = 10 * Scale; // spacing after each series/between each series label

        // Calculate the total height of the legend
        float legendHeight = 0;
        float tempX = padding;
        float rowHeight = circleDiameter + verticalSpacing;
        List<(int startIndex, float rowWidth)> rowDetails = new List<(int, float)>();

        for (int i = 0; i < series.Length; i++)
        {
            string name = series[i];

            // Measure the size of the text
            var textOptions = new TextOptions(Font);
            var textSize = TextMeasurer.MeasureSize(name, textOptions);

            // Check if we need to move to the next line
            if (tempX + circleDiameter + horizontalSpacing + textSize.Width + padding > width)
            {
                // Move to the next line
                rowDetails.Add((i, tempX));
                tempX = padding;
                legendHeight += rowHeight;
            }

            tempX += circleDiameter + horizontalSpacing + textSize.Width + seriesSpacing;
        }

        rowDetails.Add((series.Length, tempX)); // Add the last row width
        legendHeight += circleDiameter + padding;

        // Start drawing the legend from the bottom
        float currentY = height - legendHeight + padding;

        for (int row = 0; row < rowDetails.Count; row++)
        {
            float currentX = (width - rowDetails[row].rowWidth) / 2; // Center the current row

            int startIndex = row == 0 ? 0 : rowDetails[row - 1].startIndex;
            int endIndex = rowDetails[row].startIndex;

            for (int i = startIndex; i < endIndex; i++)
            {
                string name = series[i];
                var color = Rgba32.ParseHex(COLORS[i % COLORS.Length]);

                // Measure the size of the text
                var textOptions = new TextOptions(Font);
                var textSize = TextMeasurer.MeasureSize(name, textOptions);

                // Draw the color circle
                var circlePosition = new PointF(currentX + circleRadius, currentY + circleRadius);
                ctx.Fill(color, new EllipsePolygon(circlePosition, circleRadius));

                // Draw the series name
                var textPosition = new PointF(currentX + circleDiameter + horizontalSpacing, currentY);
                ctx.DrawText(name, Font, TextPen, textPosition);

                // Update the current X position
                currentX += circleDiameter + horizontalSpacing + textSize.Width + seriesSpacing;
            }

            currentY += rowHeight;
        }
        
        return (int)Math.Ceiling(legendHeight) + 5;
    }
}