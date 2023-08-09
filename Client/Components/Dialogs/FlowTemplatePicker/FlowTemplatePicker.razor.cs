
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
    private string SelectedTag = string.Empty;
    private string SelectedSubTag = string.Empty;
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
        Selected = null;
        FilterText = string.Empty;
        SelectedTag = string.Empty;
        SelectedSubTag = string.Empty;
        Visible = true;
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

    void SelectTemplate(FlowTemplateModel item, bool andAccept = false)
    {
        Selected = item;
        if (andAccept)
            _ = New();
        StateHasChanged();
    }

    async Task New()
    {
        Blocker.Show();
        
        StateHasChanged();
        try
        {
            var flowResult =
                await HttpHelper.Post<FlowTemplateModel>("/api/flow-template", Selected);
            if (flowResult.Success == false)
            {
                return;
            }
            ShowTask.SetResult(flowResult.Data);
            Visible = false;
            
        }
        finally
        {
            Blocker.Hide();
            StateHasChanged();
        }
        
    }

    void Cancel()
    {
        ShowTask.SetResult(null);
        this.Visible = false;
    }

    void ToggleTag(MouseEventArgs ev, string tag)
    {
        if (SelectedTag == tag)
        {
            SelectedTag = string.Empty;
            SelectedSubTag = string.Empty;
        }
        else
        {
            SelectedTag = tag;
            SelectedSubTag = string.Empty;
        }

        if (Selected != null && Selected.Tags.Contains(SelectedTag) == false)
            Selected = null; // clear it
    }

    void ToggleSubTag(MouseEventArgs ev, string tag)
    {
        if (SelectedSubTag == tag)
            SelectedSubTag = string.Empty;
        else
            SelectedSubTag = tag;

        if (Selected != null && Selected.Tags.Contains(SelectedSubTag) == false)
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
                x.Name.ToLowerInvariant().Contains(text) || x.Description.ToLowerInvariant().Contains(text) || x.Author.ToLowerInvariant().Contains(text))
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
                await HttpHelper.Get<List<FlowTemplateModel>>("/api/flow-template?type=" + type);
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