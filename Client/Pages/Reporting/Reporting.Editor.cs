using System.Text.Json;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.Shared.Validators;

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

        Dictionary<Guid, string> flows;
        Dictionary<Guid, string> libraries;
        try
        {
            var flowsResult = await HttpHelper.Get<Dictionary<Guid, string>>($"/api/flow/basic-list");
            flows = flowsResult.Success ? flowsResult.Data ?? new() : new();

            var librariesResult = await HttpHelper.Get<Dictionary<Guid, string>>($"/api/library/basic-list");
            libraries = librariesResult.Success ? librariesResult.Data ?? new() : new();

            if (rd.PeriodSelection)
            {
                fields.Add(new ElementField()
                {
                    InputType = FormInputType.DateRange,
                    Name = "Period"
                });
            }

            AddLibrarySelectField("Flow", flows, rd.FlowSelection, ref fields);
            AddLibrarySelectField("Library", libraries, rd.LibrarySelection, ref fields);

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
                TypeName = "Report", Title = rd.Name, Fields = fields,
                SaveLabel = "Labels.Run", CancelLabel = "Labels.Close", Large = true,
                SaveCallback = async (model) =>
                {
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
        await Editor.Open(new()
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
    }

    private void AddLibrarySelectField(string title, Dictionary<Guid, string> list, ReportSelection selection,
        ref List<ElementField> fields)
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
                fields.Add(new ElementField()
                {
                    Name = title,
                    InputType = FormInputType.Checklist,
                    Parameters = new()
                    {
                        {
                            "Options", listOptions
                        }
                    }
                });
                break;
            case ReportSelection.AnyRequired:
                fields.Add(new ElementField()
                {
                    Name = title,
                    InputType = FormInputType.Checklist,
                    Parameters = new()
                    {
                        {
                            "Options", listOptions
                        }
                    },
                    Validators = [new Required()]
                });
                break;
        }
    }
}