using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Common;

/// <summary>
/// A variable text input that shows a variables drop down when { is pushed
/// </summary>
public partial class VariableInput:ComponentBase
{
    private string _Uid = Guid.NewGuid().ToString();

    [Parameter]
    public string Uid
    {
        get => _Uid;
        set => _Uid = value;    
    }


    private Dictionary<string, object> _Variables = new Dictionary<string, object>();

    [Parameter]
    public Dictionary<string, object> Variables
    {
        get => _Variables;
        set { _Variables = value ?? new Dictionary<string, object>(); }
    }
    private List<string> VariablesFiltered { get; set; } = new List<string>();

    private ElementReference eleInput;

    private string _Value;
    [Parameter]
    public string Value
    {
        get => _Value;
        set
        {
            if (_Value == value)
                return;

            _Value = value ?? string.Empty;
            ValueChanged.InvokeAsync(_Value);
        }
    }
    [Parameter]
    public string Placeholder { get; set; }

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public EventCallback SubmitEvent { get; set; }
    [Parameter]
    public EventCallback CloseEvent { get; set; }
    
    /// <summary>
    /// Gets or sets the event that occurs on the blur event
    /// </summary>
    [Parameter] public EventCallback Blur { get; set; }
    


    /// <summary>
    /// The index in the string the variable will be inserted at
    /// </summary>
    private int VariablesIndex = 0;

    private int SelectedIndex = 0;

    private string ValueStart, ValueEnd;

    public bool VariablesShown { get; set; }

    private string FilterText = string.Empty;

    [Inject] IJSRuntime jsRuntime { get; set; }

    public void Focus()
    {
        _ = jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{Uid}').focus()");
    }


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            await jsRuntime.InvokeVoidAsync("ff.disableMovementKeys", new object[] { eleInput });
        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task OnKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "{")
        {
            // filter out any dots yet, these will only be shown if they reach that far to simplify the list
            FilterText = string.Empty;
            this.VariablesFiltered = GetFilteredList(string.Empty);
            VariablesShown = true;
            SelectedIndex = 0;
            // get the caret index position
            VariablesIndex = await GetCaretPosition();
            ValueStart = VariablesIndex == 0 ? string.Empty : this.Value.Substring(0, VariablesIndex);
            ValueEnd = this.Value.Length > VariablesIndex ? this.Value.Substring(VariablesIndex) : string.Empty;
        }
        else if (args.Key == "}")
        {
            VariablesShown = false;
            this.FilterText = string.Empty;
        }
        else if (VariablesShown)
        {
            await VariablesKeyDown(args);
        }
        else if(args.Key == "Enter")
        {
            await this.SubmitEvent.InvokeAsync();
        }
        else if(args.Key == "Escape")
        {
            await this.CloseEvent.InvokeAsync();
        }
    }

    private async Task VariablesKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "ArrowDown")
        {
            if (++SelectedIndex >= VariablesFiltered.Count)
                SelectedIndex = 0;
        }
        else if (args.Key == "ArrowUp")
        {
            if (--SelectedIndex < 0)
                SelectedIndex = VariablesFiltered.Count - 1;
        }
        else if (args.Key == "Enter" || args.Key == "Tab")
        {
            await InsertVariable(VariablesFiltered[SelectedIndex]);
        }
        else if (args.Key == "Space")
        {
            // invalid in variables, hide it
            VariablesShown = false;
        }
        else if (args.Key == "Backspace")
        {
            // check if { was deleted
            var caretPos = await GetCaretPosition();
            if (caretPos <= VariablesIndex + 1)
            {
                // { was removed
                VariablesShown = false;
                return;
            }
            // need to update the filter
            if (FilterText.Length > 0)
                FilterText = FilterText.Substring(0, FilterText.Length - 1);
            this.VariablesFiltered = GetFilteredList(FilterText);
            this.SelectedIndex = 0;

        }
        else if (args.Key.Length == 1)
        {
            FilterText += args.Key.ToLower();
            this.VariablesFiltered = GetFilteredList(FilterText);
            this.SelectedIndex = 0;
        }
    }

    private List<string> GetFilteredList(string filter)
    {
        return Variables.Where(x => {
            if (filter == string.Empty)
                return true;
            if (x.Key.StartsWith(filter) == false)
                return false;
            return true;
        })
        .Select(x =>
        {
            int index = x.Key.IndexOf(".", filter.Length);
            if (index > 0)
                return x.Key.Substring(0, index + 1);
            return x.Key;
        })
        .Distinct()
        .OrderBy(x => x).ToList();
    }


    private async Task InsertVariable(string text)
    {
        if (VariablesShown == false)
            return;

        if (text.EndsWith("."))
        {
            // part property, add more
            this.FilterText = text;
            this.VariablesFiltered = GetFilteredList(FilterText);
            this.SelectedIndex = 0;

            string newValue = ValueStart + "{" + text;
            int newCaretPos = newValue.Length;
            newValue += ValueEnd;
            this.Value = newValue;
            await Task.Delay(50);
            await this.SetCaretPosition(newCaretPos);
            return;
        }
        else
        {

            VariablesShown = false;
            string newValue = ValueStart + "{" + text + "}";
            int newCaretPos = newValue.Length;
            newValue += ValueEnd;
            this.Value = newValue;
            await Task.Delay(50);
            await this.SetCaretPosition(newCaretPos);
        }
    }

    private async Task<int> GetCaretPosition()
    {
        int position = await jsRuntime.InvokeAsync<int>("eval", new object[] { $"document.getElementById('{Uid}').selectionEnd" });
        return position;
    }

    private async Task SetCaretPosition(int position)
    {
        await jsRuntime.InvokeVoidAsync("eval", new object[] { $"document.getElementById('{Uid}').setSelectionRange({position}, {position})" });
    }

    /// <summary>
    /// Called when the input loses focus
    /// </summary>
    private async Task OnBlur()
    {
        if(VariablesShown == false)
            await this.Blur.InvokeAsync();
    }
}
