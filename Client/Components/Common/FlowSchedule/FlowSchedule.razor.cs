using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace FileFlows.Client.Components.Common;

public partial class FlowSchedule: ComponentBase
{
    private string _Value = new string('1', 672);
#pragma warning disable BL0007
    [Parameter]
    public string Value
    {
        get => _Value;
        set
        {
            string v = value;
            if (v == null || Regex.IsMatch(v, "^[01]{672}$") == false)
                v = new string('1', 672);
            if(_Value != v)
            {
                _Value = v;
                if(ValueChanged.HasDelegate)
                    ValueChanged.InvokeAsync(v);
            }
        }
    }
#pragma warning restore BL0007

    [Parameter]
    public EventCallback<string> ValueChanged{get;set; }

    [Parameter]
    public Expression<Func<string>> ValueExpression { get; set; }

    [Inject] private IJSRuntime jsRuntime { get; set; }

    private string[] DayLabels;

    private ScreenSize ScreenSize;
    private string DisplaySize;

    private bool MouseDown = false;
    private int MouseDownQh = -1;
    private int MouseOverQh = -1;
    private bool Clearing = false;

    protected override async Task OnInitializedAsync()
    {
        this.DayLabels = new[]
        {
            Translater.Instant("Enums.Days.Sun"),
            Translater.Instant("Enums.Days.Mon"),
            Translater.Instant("Enums.Days.Tue"),
            Translater.Instant("Enums.Days.Wed"),
            Translater.Instant("Enums.Days.Thu"),
            Translater.Instant("Enums.Days.Fri"),
            Translater.Instant("Enums.Days.Sat"),
        };

        this.ScreenSize = await jsRuntime.InvokeAsync<ScreenSize>("eval", new object[] { "({ width: window.innerWidth, height: window.innerHeight })" });
        if (this.ScreenSize.Width > 1280)
            DisplaySize = "xlarge";
        else if (this.ScreenSize.Width > 960)
            DisplaySize = "large";
        else if (this.ScreenSize.Width > 720)
            DisplaySize = "medium";
        else
            DisplaySize = "small";
    }

    private void OnMouseDown(MouseEventArgs args, int qh)
    {
        MouseDown = true;
        MouseDownQh = qh;
        MouseOverQh = qh;
        Clearing = Value[qh] == '1';
    }

    private void OnMouseUp(MouseEventArgs args, int qh)
    {
        MouseDown = false;
        int hlStart = Math.Min(MouseOverQh, MouseDownQh);
        int hlEnd = Math.Max(MouseOverQh, MouseDownQh);
        int hlQuarterStart = hlStart % 96;
        int hlQuarterEnd = hlEnd % 96;
        if (hlQuarterStart > hlQuarterEnd)
        {
            hlQuarterStart = hlEnd % 96;
            hlQuarterEnd = hlStart % 96;
        }
        int hlDayStart = hlStart / 96;
        int hlDayEnd = hlEnd / 96;
        var valueChars = Value.ToCharArray();
        for(int i = hlDayStart; i <= hlDayEnd; i++)
        {
            for(int j = hlQuarterStart; j <= hlQuarterEnd; j++)
            {
                valueChars[(i * 96) + j] = Clearing ? '0' : '1';
            }
        }
        this.Value = new string(valueChars);
    }
    private void OnMouseOver(MouseEventArgs args, int qh)
    {
        if (MouseDown == false)
            return;
        MouseOverQh = qh;
    }

    private bool inside = false;

    private void OnMouseLeave()
    {
        inside = false;
        //MouseDown = false;
    }
    private void OnMouseEnter(MouseEventArgs args)
    {
        if (inside)
            return;
        inside = true;
            // if they come back into the control with the mouse not down, turn it off
        if (MouseDown && args.Buttons != 1)
            MouseDown = false;
    }
}

class ScreenSize
{
    public float Width { get; set; }
    public float Height { get; set; }
}
