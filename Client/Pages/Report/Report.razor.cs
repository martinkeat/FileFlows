using System.Text.Json;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.Shared.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// Report page
/// </summary>
public partial class Report : ComponentBase
{
    /// <summary>
    /// Gets or sets the UID of the report to run
    /// </summary>
    [Parameter] public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] public NavigationManager NavigationManager { get; set; }
    
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the JS Runtime
    /// </summary>
    [Inject] public IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the report instance
    /// </summary>
    private InlineEditor Editor { get; set; }
    
    /// <summary>
    /// Gets or sets the element fields
    /// </summary>
    public List<ElementField> Fields { get; set; }
    
    /// <summary>
    /// Reference to JS Report class
    /// </summary>
    private IJSObjectReference jsReports;

    /// <summary>
    /// The model
    /// </summary>
    private ExpandoObject Model;
    /// <summary>
    /// Gets or sets the report name
    /// </summary>
    private string ReportName { get; set; }

    /// <summary>
    /// The buttons for the form
    /// </summary>
    private List<ActionButton> Buttons = new();

    /// <summary>
    /// Gets or sets if the form is loaded
    /// </summary>
    private bool Loaded = false;
    
    /// <summary>
    /// Gets or sets the HTML of the generated report
    /// </summary>
    private string Html { get; set; }
    
    /// <summary>
    /// Gets or sets if the report output should be shown
    /// </summary>
    private bool ShowReportOutput { get; set; }
    /// <summary>
    /// Indicates if this component needs rendering
    /// </summary>
    private bool _needsRendering = false;

    private string lblBack = null!, lblClose = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        lblBack = Translater.Instant("Pages.Report.Buttons.Back");
        lblClose = Translater.Instant("Labels.Close");
        var jsObjectReference = await jsRuntime.InvokeAsync<IJSObjectReference>("import",
            $"./Pages/Reporting/Reporting.razor.js?v={Globals.Version}");
        jsReports = await jsObjectReference.InvokeAsync<IJSObjectReference>("createReporting",
            [DotNetObjectReference.Create(this)]);

        var result = await HttpHelper.Get<ReportDefinition>($"/api/report/definition/{Uid}");
        if (result.Success == false)
        {
            Toast.ShowError(Translater.TranslateIfNeeded(result.Body?.EmptyAsNull() ??
                                                         "Pages.Report.Messages.FailedToFindReport"));
            NavigationManager.NavigateTo("/reporting");
            return;
        }

        var rd = result.Data;
        this.ReportName = rd.Name;

        // clone the fields as they get wiped
        var fields = new List<ElementField>();
        Blocker.Show();
        this.StateHasChanged();

        Model = new ExpandoObject();
        var model = Model as IDictionary<string, object>;
        try
        {
            var flowsResult = await HttpHelper.Get<Dictionary<Guid, string>>($"/api/flow/basic-list");
            var flows = flowsResult.Success ? flowsResult.Data ?? new() : new();

            var librariesResult = await HttpHelper.Get<Dictionary<Guid, string>>($"/api/library/basic-list");
            var libraries = librariesResult.Success ? librariesResult.Data ?? new() : new();

            var nodesResult = await HttpHelper.Get<Dictionary<Guid, string>>($"/api/node/basic-list");
            var nodes = nodesResult.Success ? nodesResult.Data ?? new() : new();

            if (rd.DefaultReportPeriod != null)
            {
                if (InputDateRange.DateRanges.TryGetValue(
                        Translater.Instant($"Labels.DateRanges.{rd.DefaultReportPeriod.Value}"), out var period))
                    model["Period"] = period;

                fields.Add(new ElementField()
                {
                    InputType = FormInputType.DateRange,
                    Name = "Period"
                });
            }

            fields.Add(new ElementField
            {
                InputType = FormInputType.Text,
                Name = "Email"
            });

            AddSelectField("Flow", flows, rd.FlowSelection, ref fields, model);
            AddSelectField("Library", libraries, rd.LibrarySelection, ref fields, model);
            AddSelectField("Node", nodes, rd.NodeSelection, ref fields, model);

            foreach (var tf in rd.Fields ?? [])
            {
                if (tf.Type == "Switch")
                {
                    fields.Add(new ElementField
                    {
                        InputType = FormInputType.Switch,
                        Name = tf.Name
                    });
                }
                else if (tf.Type == "Select")
                {
                    var listOptions =
                        JsonSerializer.Deserialize<List<ListOption>>(JsonSerializer.Serialize(tf.Parameters));
                    model[tf.Name] = listOptions.First().Value;
                    fields.Add(new ElementField
                    {
                        InputType = FormInputType.Select,
                        Name = tf.Name,
                        Parameters = new()
                        {
                            { "Options", listOptions }
                        }
                    });

                }
            }
        }
        catch (Exception ex)
        {
            // Ignored
            Logger.Instance.ILog(ex.Message);
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }

        Fields = fields;
        Buttons =
        [
            new ()
            {
                Label = "Pages.Report.Button.Generate",
                Clicked = (_, _) => _ = Generate()
            },
            new ()
            {
                Label = "Pages.Report.Button.Back",
                Clicked = (_, _) => GoBack()
            }
        ];
        Loaded = true;
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        _needsRendering = false;
        return base.OnAfterRenderAsync(firstRender);
    }
    /// <summary>
    /// The back button was clicked
    /// </summary>
    private void GoBack()
    {
        if (ShowReportOutput)
        {
            Html = string.Empty;
            ShowReportOutput = false;
            return;
        }
        NavigationManager.NavigateTo("/reporting");
    }
    
    /// <summary>
    /// The close button was clicked
    /// </summary>
    private void Close()
    {
        NavigationManager.NavigateTo("/reporting");
    }
    
    /// <summary>
    /// Waits for the component to render
    /// </summary>
    protected async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }
    
    /// <summary>
    /// Generates the report
    /// </summary>
    private async Task Generate()
    {
        bool valid = await Editor.Validate();
        if (valid == false)
            return;
        
        Blocker.Show("Generating Report");
        this.StateHasChanged();
        try
        {
            var result = await HttpHelper.Post<string>($"/api/report/generate/{Uid}", Model);
            if (result.Success == false)
            {
                Toast.ShowError(result.Body?.EmptyAsNull() ?? "Failed generating report");
                return;
            }

            Html = result.Data;
            ShowReportOutput = true;
            StateHasChanged();
            await WaitForRender();
            await jsReports.InvokeVoidAsync("initCharts");
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
        
    }

    /// <summary>
    /// Shows the generated report
    /// </summary>
    /// <param name="name">the name of the report</param>
    /// <param name="html">the HTML of the report</param>
    /// <returns>the task to await for the opened report</returns>
    private async Task ShowReport(string name, string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            Toast.ShowWarning("No matching data found.");
            return;
        }

        await Task.CompletedTask;
        // var task = Editor.Open(new()
        // {
        //     TypeName = "ReportRender", Title = name, Model = new { Html = $"<div class=\"report-output\">{html}</div>" },
        //     Large = true, ReadOnly = true,
        //     Fields =
        //     [
        //         new()
        //         {
        //             InputType = FormInputType.Html,
        //             Name = "Html"
        //         }
        //     ]
        // });

        // await Task.Delay(50);
        // await jsReports.InvokeVoidAsync("initCharts");
        //
        // await task;
    }

    /// <summary>
    /// Adds a select field
    /// </summary>
    /// <param name="title">the title of the field</param>
    /// <param name="list">the list of options</param>
    /// <param name="selection">the selection method</param>
    /// <param name="fields">the fields to update</param>
    /// <param name="model">the model to update</param>
    private void AddSelectField(string title, Dictionary<Guid, string> list, ReportSelection selection,
        ref List<ElementField> fields, IDictionary<string, object> model)
    {
        var listOptions = list.OrderBy(x => x.Value.ToLowerInvariant())
            .Select(x => new ListOption() { Label = x.Value, Value = x.Key }).ToList();
        switch (selection)
        {
            case ReportSelection.One:
                fields.Add(new ElementField()
                {
                    Name = title,
                    InputType = FormInputType.Select,
                    Parameters = new()
                    {
                        {
                            "Options", listOptions
                        }
                    },
                });
                break;
            case ReportSelection.Any:
                model[title] = listOptions.Select(x => x.Value).ToList();
                fields.Add(new ElementField()
                {
                    Name = title,
                    InputType = FormInputType.MultiSelect,
                    Parameters = new()
                    {
                        {
                            "Options", listOptions
                        }
                    },
                    Validators = [new Required()]
                });
                break;
            case ReportSelection.AnyOrAll:
                model[title] = listOptions.Select(x => x.Value).ToList();
                // model[title] = new object[] { null }; // any
                fields.Add(new ElementField()
                {
                    Name = title,
                    InputType = FormInputType.MultiSelect,
                    Parameters = new()
                    {
                        { "Options", listOptions },
                        { "AnyOrAll", true },
                        { "LabelAny", Translater.Instant("Pages.Report.Labels.Combined") }
                    }
                });
                break;
        }
    }
}