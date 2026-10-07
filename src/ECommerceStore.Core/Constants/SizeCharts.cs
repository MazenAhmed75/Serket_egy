namespace ECommerceStore.Core.Constants;

/// <summary>One row of a size chart. Every measurement is in centimetres, weight in kilograms.</summary>
public sealed record SizeChartRow(string Size, int PantsLength, int Waist, int TopChest, int TopLength, int Weight);

public sealed record SizeChart(string Gender, string Title, IReadOnlyList<SizeChartRow> Rows);

/// <summary>
/// The size guides shown on the product page. Keep the sizes here in step with <see cref="ProductSizes.All"/>.
/// The waist values were supplied in inches and are converted (1 in = 2.54 cm) and rounded to the nearest cm.
/// </summary>
public static class SizeCharts
{
    public static readonly IReadOnlyList<SizeChart> All = new[]
    {
        new SizeChart("Male", "Men's Scrubs", new[]
        {
            new SizeChartRow("M", 100, 86, 52, 70, 60),
            new SizeChartRow("L", 102, 90, 54, 72, 70),
            new SizeChartRow("XL", 104, 94, 56, 74, 75),
            new SizeChartRow("2XL", 106, 98, 58, 76, 85)
        }),
        new SizeChart("Female", "Women's Scrubs", new[]
        {
            new SizeChartRow("M", 100, 86, 54, 68, 60),
            new SizeChartRow("L", 102, 90, 56, 70, 70),
            new SizeChartRow("XL", 104, 94, 58, 72, 75),
            new SizeChartRow("2XL", 106, 98, 60, 74, 85)
        })
    };
}
