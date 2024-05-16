using System.IO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Input Text component
/// </summary>
public partial class InputIconPicker : Input<string>
{
    /// <inheritdoc />
    public override bool Focus() => FocusUid();
    Microsoft.AspNetCore.Components.Forms.InputFile fileInput;
    private bool ModalOpened = false;
    private string SelectedIcon;
    private string Filter = string.Empty;
    private string Color;
    private string Icon;
    private string IconColor;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Value?.StartsWith("data:") != true)
        {
            var parts = (Value ?? string.Empty).Split(':');
            Icon = parts.FirstOrDefault();
            IconColor = parts.Length > 1 ? parts[1] : string.Empty;
            Color = IconColor;
        }
    }

    /// <inheritdoc />
    protected override void ValueUpdated()
    {
        ClearError();
        var parts = (Value ?? string.Empty).Split(':');
        Icon = parts.FirstOrDefault();
        IconColor = parts.Length > 1 ? parts[1] : string.Empty;
    }
    
    /// <summary>
    /// Shows a dialog to choose a built-in font
    /// </summary>
    void Choose()
    {
        if (ReadOnly) 
            return;
    
        Filter = string.Empty;
        SelectedIcon = string.Empty;
        ModalOpened = true;
    }
    
    /// <summary>
    /// Shows a dialog to upload a file
    /// </summary>
    async Task Upload()
    {    
        // Programmatically trigger the file input dialog using JSInterop
        await jsRuntime.InvokeVoidAsync("eval", "document.getElementById('fileInput').click()");

    }

    async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file != null)
        {
            // Read the file as base64 string
            Value= await ConvertToBase64(file);
            ModalOpened = false;
            StateHasChanged();
        }
    }
    public static async Task<string> ConvertToBase64(IBrowserFile file)
    {
        using (var memoryStream = new MemoryStream())
        {
            await file.OpenReadStream().CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            var base64String = Convert.ToBase64String(bytes);
            return $"data:{file.ContentType};base64,{base64String}";
        }
    }

    private void SelectIcon(string icon)
        => SelectedIcon = icon;

    private void DblClick(string icon)
    {
        this.Value = icon + (string.IsNullOrWhiteSpace(Color) ? string.Empty : ":" + Color);
        ModalOpened = false;
    }

    private void Cancel()
    {
        ModalOpened = false;
    }
}