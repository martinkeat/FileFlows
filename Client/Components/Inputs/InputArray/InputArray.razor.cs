using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Linq;
using System.Threading.Tasks;

namespace FileFlows.Client.Components.Inputs;

public partial class InputArray : Input<string[]>
{
    [Parameter]
    public bool AllowDuplicates { get; set; }
    [Parameter]
    public bool EnterOnSpace { get; set; }
    private string InputText = "";
    private string PreviousInputText = "";
    public override bool Focus() => FocusUid();

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Value == null)
            Value = new string[] { };
    }

    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.ShiftKey == false && e.AltKey == false && e.CtrlKey == false)
        {
            if (e.Code == "Enter" && string.IsNullOrWhiteSpace(this.InputText))
            {
                _ = this.OnSubmit.InvokeAsync();
            }
            else if (e.Code == "Escape")
            {
                _ = OnClose.InvokeAsync();
            }
            else if (e.Code == "Enter" || (EnterOnSpace && e.Code == "Space"))
            {
                if (Add(InputText))
                    this.InputText = "";
            }
            else if (e.Code == "Backspace" && PreviousInputText == string.Empty)
            {
                if (this.Value?.Any() == true)
                {
                    this.Value = this.Value.Take(this.Value.Length - 1).ToArray();
                }
            }
        }
        PreviousInputText = InputText;
    }

    void Remove(string str)
    {
        this.Value = this.Value.Except(new[] { str }).ToArray();
    }

    bool Add(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return false;
        str = str.Trim();
        if (AllowDuplicates == false)
        {
            if (this.Value.Contains(str))
                return false;
        }
        this.Value = this.Value.Union(new[] { str }).ToArray();
        return true;
    }

    void OnBlur()
    {
        if (string.IsNullOrEmpty(InputText) == false)
        {
            if (Add(InputText))
                InputText = string.Empty;
        }
    }

    protected override void ValueUpdated()
    {
        Logger.Instance.ILog("StringArray value updated!", this.Value);
    }
}