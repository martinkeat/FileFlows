using System.IO;
using FileFlows.Client.Components.Dialogs;
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
    [Inject] IJSRuntime jsRuntime { get; set; }
    private bool ModalOpened = false;
    private string SelectedIcon;
    private string Filter = string.Empty;

    /// <inheritdoc />
    protected override void ValueUpdated()
    {
        ClearError();
    }
    
    /// <summary>
    /// Shows a dialog to choose a built-in font
    /// </summary>
    async Task Choose()
    {
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
        this.Value = icon;
        ModalOpened = false;
    }

    private void Cancel()
    {
        ModalOpened = false;
    }
}