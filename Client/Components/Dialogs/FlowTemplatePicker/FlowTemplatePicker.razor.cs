
using BlazorMonaco;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// A modal dialog for selecting a flow template
/// </summary>
public partial class FlowTemplatePicker : ComponentBase
{
    /// <summary>
    /// Gets or sets the blocker to use
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    private string lblTitle, lblDescription, lblFilter, lblNext, lblCancel;
    private string FilterText = string.Empty;
    private bool Visible { get; set; }
    TaskCompletionSource<FlowTemplateModel?> ShowTask;

    private FlowTemplateModel Selected = null;
    private List<string> SelectedTags = new();
    private List<FlowTemplateModel> FilteredTemplates;
    
    /// <summary>
    /// Gets or sets the available templates
    /// </summary>
    private List<FlowTemplateModel> Templates { get; set; }
    /// <summary>
    /// Gets or sets the standard templates
    /// </summary>
    private List<FlowTemplateModel> StandardTemplates { get; set; }
    /// <summary>
    /// Gets or sets the failure templates
    /// </summary>
    private List<FlowTemplateModel> FailureTemplates { get; set; }

    private List<string> Tags { get; set; }


    protected override void OnInitialized()
    {
        lblTitle = Translater.Instant("Dialogs.FlowTemplatePicker.Title");
        lblDescription = Translater.Instant("Dialogs.FlowTemplatePicker.Description");
        lblNext = Translater.Instant("Labels.Next");
        lblCancel = Translater.Instant("Labels.Cancel");
        lblFilter = Translater.Instant("Labels.Filter");
        App.Instance.OnEscapePushed += InstanceOnOnEscapePushed;
    }

    private void InstanceOnOnEscapePushed(OnEscapeArgs args)
    {
        if (Visible)
        {
            Cancel();
            StateHasChanged();
        }
    }

    public Task<FlowTemplateModel?> Show(FlowType type)
    {
        this.Selected = null;
        this.FilterText = string.Empty;
        this.SelectedTags.Clear();
        this.Visible = true;
        ShowTask = new TaskCompletionSource<FlowTemplateModel?>();
        Blocker.Show();
        Task.Run(async () =>
        {
            Templates = await GetTemplates(type);
            Filter();
            Tags = Templates.SelectMany(x => x.Tags).Distinct().OrderBy(x => x).ToList();
            Blocker.Hide();
            StateHasChanged();
        });
        return ShowTask.Task;
    }

    void SelectTemplate(FlowTemplateModel item)
    {
        Selected = item;
        StateHasChanged();
    }

    void New()
    {
        ShowTask.SetResult(this.Selected);
        this.Visible = false;
    }

    void Cancel()
    {
        ShowTask.SetResult(null);
        this.Visible = false;
    }

    void ToggleTag(MouseEventArgs ev, string tag)
    {
        if (ev.CtrlKey == false && SelectedTags.Contains(tag) == false)
            SelectedTags.Clear();
        
        if (SelectedTags.Contains(tag))
            SelectedTags.Remove(tag);
        else
            SelectedTags.Add(tag);
        if (Selected != null && Selected.Tags.Any(x => SelectedTags.Contains(x) == false))
            Selected = null; // clear it
    }

    private async Task FilterKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            this.FilterText = string.Empty;
            Filter();
        }
        else if (args.Key == "Enter")
            Filter();
    }

    void Filter()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            FilteredTemplates = Templates;
            return;
        }

        string text = FilterText.ToLowerInvariant();
        FilteredTemplates = Templates.Where(x =>
                x.Flow.Name.ToLowerInvariant().Contains(text) || x.Flow.Description.ToLowerInvariant().Contains(text) || x.Flow.Author.ToLowerInvariant().Contains(text))
            .ToList();
    }
    
    /// <summary>
    /// Shows the new flow editor
    /// </summary>
    public async Task<List<FlowTemplateModel>> GetTemplates(FlowType type)
    {
        if (type == FlowType.Standard && Templates != null)
            return StandardTemplates;
        if (type == FlowType.Failure && FailureTemplates != null)
            return FailureTemplates;
        //this.Blocker.Show("Pages.Flows.Messages.LoadingTemplates");
        this.StateHasChanged();
        try
        {
            var flowResult =
                await HttpHelper.Get<List<FlowTemplateModel>>("/api/flow/templates?type=" + type);
            if (flowResult.Success)
            {
                if (type == FlowType.Standard)
                    StandardTemplates = flowResult.Data ?? new();
                else
                    FailureTemplates = flowResult.Data ?? new();
            }
            else if (type == FlowType.Standard)
                StandardTemplates = new();
            else
                FailureTemplates = new();
            
            if (type == FlowType.Failure)
                return FailureTemplates;
            
            return StandardTemplates;
        }
        finally
        {
            // this.Blocker.Hide();
        }
    }
}