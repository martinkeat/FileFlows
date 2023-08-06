using System.Text.Json;
using FileFlows.Client.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NPoco;

namespace FileFlows.Client.Components.Inputs;

public delegate void DisabledChangeEvent(bool state);
public interface IInput
{
    string Label { get; set; }
    string Help { get; set; }
    string Placeholder { get; set; }
    string ErrorMessage { get; set; }
    bool HideLabel { get; set; }
    bool Disabled { get; set; }
    bool Visible { get; set; }

    void Dispose();

    EventCallback OnSubmit { get; set; }

    FileFlows.Shared.Models.ElementField Field { get; set; }

    Task<bool> Validate();

    EventHandler<bool> ValidStateChanged { get; set; }

    bool Focus();
}

public abstract class Input<T> : ComponentBase, IInput, IDisposable
{
    [CascadingParameter] protected InputRegister InputRegister { get; set; }
    [CascadingParameter] protected Editor Editor { get; set; }

    [Inject] protected IJSRuntime jsRuntime { get; set; }
    protected string Uid = System.Guid.NewGuid().ToString();
    private string _Label;
    private string _LabelOriginal;
    private string _Help;
    public EventHandler<bool> ValidStateChanged { get; set; }

    public string Suffix { get; set; }
    public string Prefix { get; set; }

    protected string LabelOriginal => _LabelOriginal;


    [Parameter] public EventCallback OnSubmit { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    [Parameter]
    public bool HideLabel { get; set; }

    [Parameter]
    public string Label
    {
        get => _Label;
        set
        {
            if (Disposed) return;
            if (_LabelOriginal == value)
                return;
            _LabelOriginal = value;
            if (_LabelOriginal.StartsWith("Flow.Parts.Script.Fields."))
            {
                // special case, thee dont have translations
                _LabelOriginal = _LabelOriginal["Flow.Parts.Script.Fields.".Length..];
                _Label = FlowHelper.FormatLabel(_LabelOriginal);
            }
            else if (Translater.NeedsTranslating(_LabelOriginal))
            {
                _Label = Translater.Instant(_LabelOriginal);
                if(string.IsNullOrEmpty(_Help))
                    _Help = Translater.Instant(_LabelOriginal + "-Help");
                Suffix = Translater.Instant(_LabelOriginal + "-Suffix");
                Prefix = Translater.Instant(_LabelOriginal + "-Prefix");
                Placeholder = Translater.Instant(_LabelOriginal + "-Placeholder").EmptyAsNull() ?? _Label;
            }
            else
            {
                _Label = value;
            }
        }
    }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    //[Parameter] // dont not make this a parameter, it sets it to false unexpectedly
    public bool Visible { get; set; }

    private ElementField _Field;

    [Parameter]
    public ElementField Field
    {
        get => _Field;
        set
        {
            if (_Field == value)
                return;
            if (_Field != null && value != null)
                return;  // field is already set and wired up, if we change the instance it will break the conditions.  this should be changing its blazor doing the changing
            _Field = value;
        }
    }

    [Parameter]
    public string Help { get => _Help; set { if (string.IsNullOrEmpty(value) == false) _Help = value; } }
    public string _Placeholder;

    [Parameter]
    public string Placeholder
    {
        get => _Placeholder;
        set { _Placeholder = value ?? ""; }
    }


    [Parameter] public List<FileFlows.Shared.Validators.Validator> Validators { get; set; }


    private string _ErrorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _ErrorMessage;
        set
        {
            _ErrorMessage = value;
        }
    }

    /// <summary>
    /// Gets the text to show as the placeholder
    /// </summary>
    /// <returns>the text to show as the placeholder</returns>
    protected string GetPlaceholder()
    {
        if (string.IsNullOrEmpty(this.Placeholder) == false)
            return this.Placeholder;
        if (this.HideLabel || this.Field?.HideLabel == true)
            return this.Label;
        return string.Empty;
    }

    protected T _Value;
    private bool _ValueUpdating = false;
    [Parameter]
    public T Value
    {
        get => _Value;
        set
        {
            try
            {
                if (Disposed) return;
                _ValueUpdating = true;

                if (_Value == null && value == null)
                    return;

                if (_Value != null && value != null && _Value.Equals(value)) return;

                bool areEqual = System.Text.Json.JsonSerializer.Serialize(_Value) ==
                                System.Text.Json.JsonSerializer.Serialize(value);
                if (areEqual ==
                    false) // for lists/arrays if they haven't really changed, empty to empty, dont clear validation
                    ErrorMessage = ""; // clear the error

                _Value = value;
                if (Editor?.Loaded != true)
                    return;
                ValueUpdated();
                ValueChanged.InvokeAsync(value);
                Field?.InvokeValueChanged(this.Editor, value);

                if (this.Field?.ChangeValues?.Any() == true)
                {
                    foreach (var cv in this.Field.ChangeValues)
                    {
                        if (cv.Matches(value))
                        {
                            if (cv.Field == null)
                            {
                                var field = Editor?.FindField(cv.Property);
                                if(field != null)
                                    cv.Field = field;
                            }

                            if (cv.Field != null)
                            {
                                cv.Field.InvokeValueChanged(this, cv.Value);
                            }
                        }
                    }
                }
            }
            finally
            {
                _ValueUpdating = false;
            }
        }
    }

    protected virtual void ValueUpdated() { }

    [Parameter]
    public EventCallback<T> ValueChanged { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if(this.Field != null)
            InputRegister.RegisterInput(this.Field.Uid, this);
        this.Visible = true;

        if (this.Field != null)
        {
            this.Field.DisabledChange += Field_DisabledChange;

            if (this.Field.DisabledConditions?.Any() == true)
            {
                bool disabled = false;
                foreach (var condition in this.Field.DisabledConditions)
                    disabled |= condition.IsMatch;
                this.Disabled = disabled;
            }

            this.Field.ConditionsChange += Field_ConditionsChange;
            if (this.Field.Conditions?.Any() == true)
            {
                bool visible = true;
                foreach (var condition in this.Field.Conditions)
                    visible &= condition.IsMatch == false; // conditions IsMatch stores the inverse for Disabled states, for condtions we want the inverse
                this.Visible = visible;
            }
            
            this.Field.ValueChanged += FieldOnValueChanged;

            if (string.IsNullOrEmpty(Field.Description) == false)
                this.Help = Field.Description;
        }
    }

    private void FieldOnValueChanged(object sender, object value)
    {
        if (Disposed) return;
        if (_ValueUpdating)
            return;
        if (value is JsonElement je)
        {
            if (typeof(T) == typeof(int))
                value = je.GetInt32();
            else if (typeof(T) == typeof(string))
                value = je.GetString();
            else if (typeof(T) == typeof(bool))
                value = je.GetBoolean();
        }

        try
        {
            this.Value = (T)value;
        }
        catch (InvalidCastException ex)
        {
            Logger.Instance.ILog($"Could not cast '{value.GetType().FullName}' to '{typeof(T).FullName}'");
        }
    }

    private void Field_DisabledChange(bool state)
    {
        if (Disposed) return;
        if(this.Disabled != state)
        {
            this.Disabled = state;
            this.StateHasChanged();
        }
    }

    private void Field_ConditionsChange(bool state)
    {
        if (Disposed) return;
        if (this.Field.Conditions.Count > 1)
        {
            state = true;
            foreach (var condition in this.Field.Conditions)
            {
                state &= condition.IsMatch == false; // is false since condition was for disabled state
            }
        }
        if(this.Visible != state)
        {
            this.Visible = state;
            this.StateHasChanged();
        }
    }

    protected void ClearError() => this.ErrorMessage = "";

    public virtual async Task<bool> Validate()
    {
        if (Disposed) return true;
        if (this.Validators?.Any() != true)
            return true;
        if (this.Visible == false)
            return true;

        bool isValid = string.IsNullOrEmpty(ErrorMessage);
        foreach (var val in this.Validators)
        {
            var validResult = await val.Validate(this.Value);
            if (validResult.Valid == false)
            {
                ErrorMessage = validResult.Error?.EmptyAsNull() ?? Translater.Instant($"Validators.{val.Type}", val);
                this.StateHasChanged();
                if (isValid)
                    ValidStateChanged?.Invoke(this, false);
                Logger.Instance.DLog($"Invalid '{this.Label}' validator: " + val.GetType().FullName);
                return false;
            }
        }
        if(isValid == false)
            ValidStateChanged?.Invoke(this, true);
        return true;
    }

    public virtual bool Focus() => false;

    protected bool FocusUid()
    {
        if (Disposed) return false;
        _ = jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{Uid}').focus()");
        return true;
    }

    private bool Disposed = false;
    public virtual void Dispose()
    {
        Disposed = true;
        if (this.Field != null)
        {
            this.Field.ConditionsChange -= Field_ConditionsChange;
            this.Field.DisabledChange -= Field_DisabledChange;
            this.Field.ValueChanged -= FieldOnValueChanged;
            this.Field = null;
        }
    }
}