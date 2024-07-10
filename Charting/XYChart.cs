namespace FileFlows.Charting;

public class XYChart: ImageChart
{
    /// <summary>
    /// The frequency of the yAxis Labels
    /// </summary>
    protected const int yAxisLabelFrequency = 4;
    /// <summary>
    /// THe offset of the yAxis labels
    /// </summary>
    protected const int yAxisLabelOffset = 10;
    /// <summary>
    /// The background color in the area of the chart
    /// </summary>
    protected Rgba32 ChartAreaBackgroundColor = Rgba32.ParseHex("#3b3e41");

    /// <summary>
    /// Calculates the width needed for the y-axis labels and where the x-axis should start
    /// </summary>
    /// <param name="ctx">the image context</param>
    /// <param name="maxValue">the maximum value</param>
    /// <param name="yAxisFormatter">the formatter for the y-axis labels</param>
    /// <returns>the X start position</returns>
    protected int CalculateXAxisStart(IImageProcessingContext ctx, double maxValue, string? yAxisFormatter)
    {
        float width = 0;
        for (int i = 0; i <= yAxisLabelFrequency; i++)
        {
            double value = (maxValue / yAxisLabelFrequency) * i;

            // Format y-axis label
            object yValue = string.IsNullOrWhiteSpace(yAxisFormatter)
                ? (object)Convert.ToInt64(value)
                : (object)value;
            string yLabel = ChartFormatter.Format(yValue, yAxisFormatter, axis: true);
            
            // Measure the size of the text
            var textOptions = new TextOptions(Font);
            var textSize = TextMeasurer.MeasureSize(yLabel, textOptions);

            width = Math.Max(width, textSize.Width);
        }

        return (int)width + 10;
    }

    /// <summary>
    /// Draws the y-axis
    /// </summary>
    /// <param name="ctx">the image processing context used for drawing</param>
    /// <param name="chartStartX">the position where the chart x starts</param>
    /// <param name="chartStartY">the position where the chart y starts</param>
    /// <param name="chartWidth">the width of the chart</param>
    /// <param name="chartHeight">the height of the chart</param>
    /// <param name="maxValue">the maximum value of the chart</param>
    /// <param name="yAxisFormatter">the formatter to use for the y-axis labels</param>
    protected void DrawYAxis(IImageProcessingContext ctx, int chartStartX, int chartStartY, int chartWidth, int chartHeight, double maxValue, string? yAxisFormatter)
    {

        int yAxisHeight = chartHeight;// Define font options with right alignment

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
            object yValue = string.IsNullOrWhiteSpace(yAxisFormatter) ? (object)Convert.ToInt64(value) : (object)value;
            string yLabel = ChartFormatter.Format(yValue, yAxisFormatter, axis: true);

            // Measure the size of the text
            var textOptions = new TextOptions(Font);
            var textSize = TextMeasurer.MeasureSize(yLabel, textOptions);

            // Calculate the position for right-aligned text
            var labelPosition = new PointF(chartStartX - yAxisLabelOffset - textSize.Width, y - textSize.Height / 2 - 2);
            
            // Draw y-axis label
            ctx.DrawText(yLabel, Font, TextBrush, TextPen, labelPosition);
        }
        // Draw y-axis main label if provided
        // if (!string.IsNullOrEmpty(chartData.YAxisLabel))
        // {
        //     ctx.DrawText(chartData.YAxisLabel, SystemFonts.CreateFont("Arial", 12), Rgba32.ParseHex(foregroundColor), new PointF(chartStartX - yAxisLabelOffset - 30, chartStartY + chartHeight / 2));
        // }
    }
}