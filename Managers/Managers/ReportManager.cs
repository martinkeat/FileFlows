using System.Reflection;
using FileFlows.DataLayer.Reports;

namespace FileFlows.Managers;

/// <summary>
/// Manager for reports
/// </summary>
public class ReportManager
{
    /// <summary>
    /// Gets all the report definitions 
    /// </summary>
    /// <returns>all the report definitions</returns>
    public List<ReportDefinition> GetReports()
    {
        var reports = Report.GetReports();
        var results = new List<ReportDefinition>();
        foreach(var report in reports)
        {
            var rd = new ReportDefinition();
            rd.Uid = report.Uid;
            rd.Name = report.Name;
            rd.Description = report.Description;
            rd.Icon = report.Icon;
            rd.DefaultReportPeriod = report.DefaultReportPeriod;
            rd.FlowSelection = report.FlowSelection;
            rd.LibrarySelection = report.LibrarySelection;
            rd.NodeSelection = report.NodeSelection;;
            rd.Direction = report.Direction;
            rd.Fields = new();
            var props = report.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var prop in props)
            {
                if (prop.CanWrite == false || prop.CanRead == false)
                    continue;
                var tf = new TemplateField();
                if (prop.PropertyType == typeof(bool))
                    tf.Type = "Switch";
                else if (prop.PropertyType == typeof(int))
                    tf.Type = "Int";
                else if (prop.PropertyType == typeof(string))
                    tf.Type = "String";
                else if (prop.PropertyType.IsEnum)
                {
                    tf.Type = "Select";
                    
                    tf.Parameters = Enum.GetValues(prop.PropertyType).Cast<object>()
                        .Select(enumValue => new ListOption
                        {
                            Label = enumValue.ToString(),  // Set label as string representation of enum value
                            Value = enumValue               // Set value as enum value itself
                        })
                        .ToList();
                }
                else
                    continue;
                tf.Name = prop.Name;
                tf.Label = prop.Name;
                rd.Fields.Add(tf);
            }

            results.Add(rd);
        }

        return results;
    }

    /// <summary>
    /// Generates the report
    /// </summary>
    /// <param name="uid">the UID of the report</param>
    /// <param name="emailing">if this report is being emailed</param>
    /// <param name="model">the report model</param>
    /// <returns>the reports HTML</returns>
    public async Task<Result<string>> Generate(Guid uid, bool emailing, Dictionary<string, object> model)
    {
        var report = Report.GetReports().FirstOrDefault(x => x.Uid == uid);
        if (report == null)
            return Result<string>.Fail("Report not found");
        
        return await report.Generate(model, emailing: emailing);
    }

    /// <summary>
    /// Gets the reports name
    /// </summary>
    /// <param name="uid">the UID of the reprot</param>
    /// <returns>the reports name, or null if not found</returns>
    public string? GetReportName(Guid uid)
        => Report.GetReports().FirstOrDefault(x => x.Uid == uid)?.Name;
}