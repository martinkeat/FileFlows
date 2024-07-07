using FileFlows.DataLayer.Reports;
using FileFlows.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using ServiceLoader = FileFlows.RemoteServices.ServiceLoader;

namespace FileFlows.Server.Workers;

/// <summary>
/// Worker that sends the scheduled reports
/// </summary>
public class ScheduledReportWorker() : ServerWorker(ScheduleType.Hourly, 1)
{
    /// <inheritdoc />
    protected override void ExecuteActual(Settings settings)
    {
        if (LicenseHelper.IsLicensed(LicenseFlags.Reporting) == false)
            return; // not licensed
        
        #if(!DEBUG)
        if (DateTime.Now.Hour != 1)
            return; // only run this at 1 am
        #endif

        var service = ServiceLoader.Load<ScheduledReportService>();
        var reports = service.GetAll().Result.Where(x => x.Enabled).ToList();
        if (reports.Count == 0)
            return;
            
        var manager = new ReportManager();

        foreach (var report in reports)
        {
#if(!DEBUG)
            if (report.LastSentUtc > DateTime.UtcNow.AddHours(-12))
                continue; // prevent the system being reboot and sending the same report multiple times
#endif
            
            switch (report.Schedule)
            {
                case ReportSchedule.Daily:
                    break;
                case ReportSchedule.Weekly:
                    if ((int)DateTime.Now.DayOfWeek != report.ScheduleInterval)
                        continue;
                    break;
                case ReportSchedule.Monthly:
                    int currentDay = DateTime.Now.Day;
                    int daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
    
                    if (report.ScheduleInterval <= daysInMonth)
                    {
                        // Trigger on the ScheduleInterval day of the month
                        if (currentDay != report.ScheduleInterval)
                            continue;
                    }
                    else
                    {
                        // If ScheduleInterval exceeds the days in the month, trigger on the last day of the month
                        if (currentDay != daysInMonth)
                            continue;
                    }
                    break;
            }

            Dictionary<string, object> model = new();
            model["Flow"] = report.Flows;
            model["Node"] = report.Nodes;
            model["Library"] = report.Libraries;
            model["Direction"] = report.Direction;

            try
            {
                var result = manager.Generate(report.Report.Uid, true, model).Result;
                if (result.Failed(out var rError))
                {
                    _ = ServiceLoader.Load<NotificationService>()
                        .Record(NotificationSeverity.Warning, $"Scheduled Report '{report.Name}' failed to generate",
                            rError);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(result.Value))
                {
                    _ = ServiceLoader.Load<NotificationService>()
                        .Record(NotificationSeverity.Information,
                            $"Scheduled Report '{report.Name}' had not matching data", rError);
                    return;
                }

                _ = service.Email(report.Recipients, report.Name, result.Value);
            }
            catch (Exception ex)
            {
                Logger.Instance.WLog($"Failed running scheduled report '{report.Name}': {ex.Message}");
            }

            report.LastSentUtc = DateTime.UtcNow;
            service.Update(report, null).Wait();
        }
    }
}