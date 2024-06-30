using BlazorDateRangePicker;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input date range component
/// </summary>
public partial class InputDateRange : Input<DateRange>
{
    public void OnRangeSelect(DateRange range)
        => Value = range;

    private static Dictionary<string, DateRange> _DateRanges;

    /// <summary>
    /// Gets the data ranges used in search forms
    /// </summary>
    public static Dictionary<string, DateRange> DateRanges
    {
        get
        {
            if (_DateRanges == null)
            {
                _DateRanges = new Dictionary<string, DateRange>
                {
                    {
                        Translater.Instant("Labels.DateRanges.AnyTime"), new DateRange
                        {
                            Start = DateTimeOffset.MinValue,
                            End = DateTimeOffset.MaxValue
                        }
                    },
                    {
                        Translater.Instant("Labels.DateRanges.Today"), new DateRange
                        {
                            Start = DateTime.Today,
                            End = DateTime.Today.AddDays(1).AddTicks(-1)
                        }
                    },
                    {
                        Translater.Instant("Labels.DateRanges.Yesterday"), new DateRange
                        {
                            Start = DateTime.Today.AddDays(-1),
                            End = DateTime.Today.AddTicks(-1)
                        }
                    },
                    {
                        Translater.Instant("Labels.DateRanges.Last24Hours"), new DateRange
                        {
                            Start = DateTime.Now.AddHours(-24),
                            End = DateTime.Now
                        }
                    },
                    {
                        Translater.Instant("Labels.DateRanges.Last7Days"), new DateRange
                        {
                            Start = DateTime.Now.AddDays(-7),
                            End = DateTime.Now
                        }
                    },
                    {
                        Translater.Instant("Labels.DateRanges.Last31Days"), new DateRange
                        {
                            Start = DateTime.Now.AddDays(-31),
                            End = DateTime.Now
                        }
                    },
                    {
                        Translater.Instant("Labels.DateRanges.Last3Months"), new DateRange
                        {
                            Start = DateTime.Today.AddMonths(-3),
                            End = DateTime.Today.AddTicks(-1)
                        }
                    },
                    {
                        Translater.Instant("Labels.DateRanges.Last6Months"), new DateRange
                        {
                            Start = DateTime.Today.AddMonths(-6),
                            End = DateTime.Today.AddTicks(-1)
                        }
                    },
                    {
                        Translater.Instant("Labels.DateRanges.Last12Months"), new DateRange
                        {
                            Start = DateTime.Today.AddMonths(-12),
                            End = DateTime.Today.AddTicks(-1)
                        }
                    }
                };
            }

            return _DateRanges;
        }
    }
}