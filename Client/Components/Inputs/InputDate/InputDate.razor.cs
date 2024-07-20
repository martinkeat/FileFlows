using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input date component
/// </summary>
public partial class InputDate : Input<DateTime>
{
    /// <inheritdoc />
    public override bool Focus() => FocusUid();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Value < new DateTime(1970, 1, 1))
        {
            Value = new DateTime(2020, 1, 1).ToUniversalTime();
        }
    }


    /// <summary>
    /// Gets or sets the local date
    /// </summary>
    public DateTime LocalDate
    {
        get => Value.ToLocalTime();
        set
        {
            Value = value.ToUniversalTime();
        }
    }
}