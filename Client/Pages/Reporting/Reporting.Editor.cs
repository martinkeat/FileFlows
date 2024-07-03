using System.Text.Json;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.Shared.Validators;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// The editor for a report
/// </summary>
public partial class Reporting
{
    /// <summary>
    /// Launches the report
    /// </summary>
    /// <param name="rd">the report definition</param>
    private Task Launch(ReportDefinition rd)
        => Edit(rd);
    
    /// <inheritdoc />
    public override async Task<bool> Edit(ReportDefinition rd)
    {
        // clone the fields as they get wiped
        var fields = new List<ElementField>();
        Blocker.Show();
        this.StateHasChanged();
        Data.Clear();


        IDictionary<string, object> model = new ExpandoObject() as IDictionary<string, object>;
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
                        Parameters = new ()
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

        if (fields.Count == 0)
        {
            // go straight to execution
            var htmlResult = await GenerateReportHtml(rd.Uid, null);
            if (htmlResult.Failed(out var error))
            {
                Toast.ShowError(error);
                return false;
            }
            await ShowReport(rd.Name, htmlResult.Value);
        }
        else
        {
            await ReportFormEditor.Open(new()
            {
                TypeName = "Pages.Report", Title = rd.Name, Fields = fields, Model = model,
                SaveLabel = "Labels.Run", CancelLabel = "Labels.Close", Large = true,
                SaveCallback = async (model) =>
                {
                    if (model is IDictionary<string, object> dict && dict.TryGetValue("Email", out var oEmail) && oEmail is string email )
                    {
                        _ = HttpHelper.Post<string>($"/api/report/generate/{rd.Uid}", model);
                        Toast.ShowInfo(Translater.Instant("Pages.Report.Messages.ReportEmailed",
                            new { email }));
                        return true; // email reports we do close
                    }
                    var htmlResult = await GenerateReportHtml(rd.Uid, model);
                    if (htmlResult.Failed(out var error))
                    {
                        Toast.ShowError(error);
                        return false;
                    }
                    await ShowReport(rd.Name, htmlResult.Value);
                    // we dont close the report field, this way the user can re-run the report with slightly modified parameters
                    return false;
                }
            });
        }

        return false; // we don't need to reload the list
    }

    private async Task<Result<string>> GenerateReportHtml(Guid uid, object? model)
    {
        Blocker.Show("Generating Report");
        this.StateHasChanged();
        try
        {
            var result = await HttpHelper.Post<string>($"/api/report/generate/{uid}", model);
            if (result.Success == false)
                return Result<string>.Fail(result.Body?.EmptyAsNull() ?? "Failed generating report");

            return result.Data;
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
        var task = Editor.Open(new()
        {
            TypeName = "ReportRender", Title = name, Model = new { Html = $"<div class=\"report-output\">{html}</div>" },
            Large = true, ReadOnly = true,
            Fields =
            [
                new()
                {
                    InputType = FormInputType.Html,
                    Name = "Html"
                }
            ]
        });

        await Task.Delay(50);
        await jsReports.InvokeVoidAsync("initCharts");

        await task;
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