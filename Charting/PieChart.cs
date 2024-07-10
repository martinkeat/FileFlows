namespace FileFlows.Charting;

/// <summary>
/// Pie Chart
/// </summary>
public class PieChart : ImageChart
{
    /// <summary>
    /// Generates a pie chart.
    /// </summary>
    /// <param name="chartData">The chart data.</param>
    /// <param name="customWidth">The width of the image.</param>
    /// <param name="customHeight">The height of the image.</param>
    /// <returns>The base64 encoded image tag.</returns>
    public string GenerateImage(PieChartData chartData, int? customWidth = null, int? customHeight = null)
    {
        // Initialize ImageSharp Image
        var (width, height) = GetImageSize(customWidth, customHeight);
        using var image = new Image<Rgba32>(width, height);

        // Define chart dimensions and positions
        int chartStartX = 60;
        int chartStartY = 10;
        int chartEndX = 20;
        int legendWidth = CalculateLegendWidth(chartData, width);
        int chartWidth = width - chartStartX - chartEndX - legendWidth;
        int chartHeight = height - chartStartY - 10; // Adjusted for labels

        // Calculate total value
        double totalValue = chartData.Data.Values.Sum();

        // Draw chart elements using Mutate()
        image.Mutate(ctx =>
        {
            // Draw pie chart
            DrawPie(ctx, chartData, chartStartX, chartStartY, chartWidth, chartHeight, totalValue);

            // Draw legend
            DrawLegend(ctx, chartData, width - legendWidth - 10, chartStartY);
        });

        return ImageToBase64ImgTag(image);
    }

    /// <summary>
    /// Draws the pie chart
    /// </summary>
    /// <param name="ctx">The image processing context used for drawing</param>
    /// <param name="chartData">The chart data</param>
    /// <param name="chartStartX">The X starting point of the chart</param>
    /// <param name="chartStartY">The Y starting point of the chart</param>
    /// <param name="chartWidth">The width of the chart</param>
    /// <param name="chartHeight">The height of the chart</param>
    /// <param name="totalValue">The total value of all segments</param>
    private void DrawPie(IImageProcessingContext ctx, PieChartData chartData, int chartStartX, int chartStartY, int chartWidth, int chartHeight, double totalValue)
    {
        float centerX = chartStartX + chartWidth / 2f;
        float centerY = chartStartY + chartHeight / 2f;
        float radius = Math.Min(chartWidth, chartHeight) / 2f;

        float startAngle = 0;
        int i = 0;

        foreach (var kvp in chartData.Data)
        {
            string label = kvp.Key;
            double value = kvp.Value;
            float sweepAngle = (float)(value / totalValue * 360);

            // Draw pie segment
            var segmentColor = Rgba32.ParseHex(COLORS[i % COLORS.Length]);
            var pieSlice = CreatePieSlice(centerX, centerY, radius, startAngle, sweepAngle);
            ctx.Fill(segmentColor, pieSlice);

            // Draw label
            float midAngle = startAngle + sweepAngle / 2;
            float labelX = centerX + (float)(Math.Cos(midAngle * Math.PI / 180) * radius * 0.7);
            float labelY = centerY + (float)(Math.Sin(midAngle * Math.PI / 180) * radius * 0.7);

            // var textOptions = new TextOptions(Font);
            // var textSize = TextMeasurer.MeasureSize(label, textOptions);
            // var labelPosition = new PointF(labelX - textSize.Width / 2, labelY - textSize.Height / 2);
            // ctx.DrawText(label, Font, Color.Black, labelPosition);

            startAngle += sweepAngle;
            i++;
        }
    }


    private IPath CreatePieSlice(float centerX, float centerY, float radius, float startAngle, float sweepAngle)
    {
        var pathBuilder = new PathBuilder();

        // Convert start and sweep angles to radians for trigonometric calculations
        float startAngleRad = startAngle * (float)Math.PI / 180;
        float sweepAngleRad = sweepAngle * (float)Math.PI / 180;

        // Calculate start and end points
        PointF startPoint = new PointF(centerX + radius * (float)Math.Cos(startAngleRad), centerY + radius * (float)Math.Sin(startAngleRad));
        PointF endPoint = new PointF(centerX + radius * (float)Math.Cos(startAngleRad + sweepAngleRad), centerY + radius * (float)Math.Sin(startAngleRad + sweepAngleRad));

        // Add lines and arc to path
        pathBuilder.AddLine(new PointF(centerX, centerY), startPoint);
        pathBuilder.AddArc(new RectangleF(centerX - radius, centerY - radius, 2 * radius, 2 * radius), 0, startAngle, sweepAngle);
        pathBuilder.AddLine(endPoint, new PointF(centerX, centerY));
        pathBuilder.CloseFigure();

        return pathBuilder.Build();
    }
        

    /// <summary>
    /// Calculates the width needed for the legend.
    /// </summary>
    /// <param name="chartData">The chart data</param>
    /// <param name="imageWidth">The width of the image</param>
    /// <returns>The width needed for the legend</returns>
    private int CalculateLegendWidth(PieChartData chartData, int imageWidth)
    {
        float padding = 5 * Scale;
        float circleDiameter = 10 * Scale;

        float legendWidth = 0;
        foreach (var kvp in chartData.Data)
        {
            string label = kvp.Key;

            // Measure the size of the text
            var textOptions = new TextOptions(Font);
            var textSize = TextMeasurer.MeasureSize(label, textOptions);

            legendWidth = Math.Max(legendWidth, textSize.Width + circleDiameter + padding * 2);
        }

        return (int)Math.Ceiling(legendWidth);
    }

    /// <summary>
    /// Draws the legend
    /// </summary>
    /// <param name="ctx">The image processing context used for drawing</param>
    /// <param name="chartData">The chart data</param>
    /// <param name="legendStartX">The X starting point of the legend</param>
    /// <param name="legendStartY">The Y starting point of the legend</param>
    private void DrawLegend(IImageProcessingContext ctx, PieChartData chartData, int legendStartX, int legendStartY)
    {
        float padding = 5 * Scale;
        float verticalSpacing = 5 * Scale;
        float circleDiameter = 10 * Scale;
        float circleRadius = circleDiameter / 2;

        float currentY = legendStartY + padding;

        int i = 0;
        foreach (var kvp in chartData.Data)
        {
            string label = kvp.Key;

            // Draw the color circle
            var circlePosition = new PointF(legendStartX + circleRadius, currentY + circleRadius);
            ctx.Fill(Rgba32.ParseHex(COLORS[i % COLORS.Length]), new EllipsePolygon(circlePosition, circleRadius));

            // Draw the series name
            var textPosition = new PointF(legendStartX + circleDiameter + padding, currentY);
            ctx.DrawText(label, Font, TextBrush, TextPen, textPosition);

            currentY += circleDiameter + verticalSpacing;
            i++;
        }
    }
}