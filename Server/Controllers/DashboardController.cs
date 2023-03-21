using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.Server.Services;
using FileFlows.Shared.Models;
using FileFlows.Shared.Widgets;
using Microsoft.AspNetCore.Mvc;

namespace FileFlows.Server.Controllers;

/// <summary>
/// Controller for the dashboard
/// </summary>
[Route("/api/dashboard")]
public class DashboardController : Controller
{
    /// <summary>
    /// Get all dashboards in the system
    /// </summary>
    /// <returns>all dashboards in the system</returns>
    [HttpGet]
    public IEnumerable<Dashboard> GetAll()
    {
        var dashboards = new DashboardService().GetAll()
            .OrderBy(x => x.Name.ToLower()).ToList();
        if (dashboards.Any() == false)
        {
            // add default
        }
        return dashboards;
    }

    /// <summary>
    /// Get a list of all dashboards
    /// </summary>
    /// <returns>all dashboards in the system</returns>
    [HttpGet("list")]
    public IEnumerable<ListOption> ListAll()
    {
        var dashboards = new DashboardService().GetAll()
            .OrderBy(x => x.Name.ToLower()).Select(x => new ListOption
        {
            Label = x.Name,
            Value = x.Uid
        }).ToList();
        // add default
        dashboards.Insert(0, new ()
        {
            Label = Dashboard.DefaultDashboardName,
            Value = Dashboard.DefaultDashboardUid
        });
        return dashboards;
    }

    /// <summary>
    /// Get a dashboard
    /// </summary>
    /// <param name="uid">The UID of the dashboard</param>
    /// <returns>The dashboard instance</returns>
    [HttpGet("{uid}/Widgets")]
    public IEnumerable<WidgetUiModel> Get(Guid uid)
    {
        var db = new DashboardService().GetByUid(uid);;
        if ((db == null || db.Uid == Guid.Empty) && uid == Dashboard.DefaultDashboardUid)
            db = Dashboard.GetDefaultDashboard(DbHelper.UseMemoryCache == false);
        else if (db == null)
            throw new Exception("Dashboard not found");
        List<WidgetUiModel> Widgets = new List<WidgetUiModel>();
        foreach (var p in db.Widgets)
        {
            try
            {
                var pd = WidgetDefinition.GetDefinition(p.WidgetDefinitionUid);
                WidgetUiModel pui = new()
                {
                    X = p.X,
                    Y = p.Y,
                    Height = p.Height,
                    Width = p.Width,
                    Uid = p.WidgetDefinitionUid,

                    Flags = pd.Flags,
                    Name = pd.Name,
                    Type = pd.Type,
                    Url = pd.Url,
                    Icon = pd.Icon
                };
                #if(DEBUG)
                pui.Url = "http://localhost:6868" + pui.Url;
                #endif
                Widgets.Add(pui);
            }
            catch (Exception)
            {
                // can throw if Widget definition is not found
                Logger.Instance.WLog("Widget definition not found: " + p.WidgetDefinitionUid);
            }
        }

        return Widgets;
    }

    /// <summary>
    /// Delete a dashboard from the system
    /// </summary>
    /// <param name="uid">The UID of the dashboard to delete</param>
    /// <returns>an awaited task</returns>
    [HttpDelete("{uid}")]
    public Task Delete([FromRoute] Guid uid)
        => new DashboardService().Delete(uid);
    
    /// <summary>
    /// Saves a dashboard
    /// </summary>
    /// <param name="model">The dashboard being saved</param>
    /// <returns>The saved dashboard</returns>
    [HttpPut]
    public Dashboard Save([FromBody] Dashboard model)
    {
        new DashboardService().Update(model);
        return model;
    }

    /// <summary>
    /// Saves a dashboard
    /// </summary>
    /// <param name="uid">The UID of the dashboard</param>
    /// <param name="widgets">The Widgets to save</param>
    /// <returns>The saved dashboard</returns>
    [HttpPut("{uid}")]
    public Dashboard Save([FromRoute] Guid uid, [FromBody] List<Widget> widgets)
    {
        var service = new DashboardService();
        var dashboard = service.GetByUid(uid);
        if (dashboard == null)
            throw new Exception("Dashboard not found");
        dashboard.Widgets = widgets ?? new List<Widget>();
        service.Update(dashboard);
        return dashboard;
    }
}