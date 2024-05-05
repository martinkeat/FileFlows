using FileFlows.Server.Services;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Server.Views.Shared;

/// <summary>
/// Loading component shown while the application is starting
/// </summary>
public partial class Loading : ComponentBase
{
    /// <summary>
    /// The current status message
    /// </summary>
    private string Status = "Initializing";
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        var service = ServiceLoader.Load<StartupService>();
        Status = service.CurrentStatus;
        service.OnStatusUpdate += (message) =>
        {
            _ = InvokeAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(message))
                    return;
                
                Status = message; 
                StateHasChanged();
            });
        };
    }

}