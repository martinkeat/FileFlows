using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using FileFlows.Shared.Widgets;

namespace FileFlows.Shared.Models;

/// <summary>
/// Dashboard that shows Widgets
/// </summary>
public class Dashboard: FileFlowObject
{
    private List<Widget> _widgets = new List<Widget>();

    /// <summary>
    /// The name of the default dashboard
    /// </summary>
    public const string DefaultDashboardName = "Default Dashboard";
    
    /// <summary>
    /// The UID of the default dashboard
    /// </summary>
    public static readonly Guid DefaultDashboardUid = new Guid("bed286d9-68f0-48a8-8c6d-05ec6f81d67c");
        
    /// <summary>
    /// Gets or sets the widgets on this dashboard
    /// </summary>
    public List<Widget> Widgets
    {
        get => _widgets;
        set
        {
            _widgets.Clear();
            if(value != null)
                _widgets.AddRange(value);
        }
    }

    /// <summary>
    /// Gets a default dashboard
    /// </summary>
    /// <param name="usingExternalDatabase">If the the system is using an external database or not</param>
    /// <param name="enabledProcessingNodes">The number of enabled processing nodes</param>
    /// <returns>the default dashboard</returns>
    public static Dashboard GetDefaultDashboard(bool usingExternalDatabase, int enabledProcessingNodes)
    {
        var db = new Dashboard();
        db.Name = DefaultDashboardName;
        db.Uid = DefaultDashboardUid;
        db.Widgets = new();
        int rowIndex = 0;
        
        // top row
        db.Widgets.Add(new ()
        {
            Height = 1, Width = 3,
            Y = rowIndex, X = 0,
            WidgetDefinitionUid = CpuUsage.WD_UID
        });
        db.Widgets.Add(new ()
        {
            Height = 1, Width = 3,
            Y = rowIndex, X = 3,
            WidgetDefinitionUid = MemoryUsage.WD_UID
        });
        db.Widgets.Add(new ()
        {
            Height = 1, Width = 3,
            Y = rowIndex, X = 6,
            WidgetDefinitionUid = TempStorage.WD_UID
        });
        
        if (enabledProcessingNodes > 1)
        {
            db.Widgets.Add(new()
            {
                Height = 1, Width = 3,
                Y = rowIndex, X = 9,
                WidgetDefinitionUid = ProcessingNodes.WD_UID
            });
        }
        else if (usingExternalDatabase)
        {
            db.Widgets.Add(new()
            {
                Height = 1, Width = 3,
                Y = rowIndex, X = 9,
                WidgetDefinitionUid = OpenDatabaseConnections.WD_UID
            });
        }
        else
        {
            db.Widgets.Add(new()
            {
                Height = 1, Width = 3,
                Y = rowIndex, X = 9,
                WidgetDefinitionUid = LogStorage.WD_UID
            });
        }

        ++rowIndex;
        
        // executing
        db.Widgets.Add(new ()
        {
            Height = 2, Width = 12,
            Y = rowIndex, X = 0,
            WidgetDefinitionUid = Processing.WD_UID
        });
        rowIndex += 2;
        
        // library files row
        db.Widgets.Add(new ()
        {
            Height = 2, Width = 6,
            Y = rowIndex, X = 0,
            WidgetDefinitionUid = FilesUpcoming.WD_UID
        });
        db.Widgets.Add(new ()
        {
            Height = 2, Width = 6,
            Y = rowIndex, X = 6,
            WidgetDefinitionUid = FilesRecentlyFinished.WD_UID
        });
        rowIndex += 2;


        // codecs and times row
        db.Widgets.Add(new()
        {
            Height = 2, Width = 6,
            Y = rowIndex, X = 0,
            WidgetDefinitionUid = Codecs.WD_UID
        });
        db.Widgets.Add(new()
        {
            Height = 2, Width = 6,
            Y = rowIndex, X = 6,
            WidgetDefinitionUid = ProcessingTimes.WD_UID
        });

        rowIndex += 2;

        // containers, resolution, processing times row
        db.Widgets.Add(new()
        {
            Height = 2, Width = 4,
            Y = rowIndex, X = 0,
            WidgetDefinitionUid = VideoContainers.WD_UID
        });
        db.Widgets.Add(new()
        {
            Height = 2, Width = 4,
            Y = rowIndex, X = 4,
            WidgetDefinitionUid = VideoResolution.WD_UID
        });
        db.Widgets.Add(new()
        {
            Height = 2, Width = 4,
            Y = rowIndex, X = 8,
            WidgetDefinitionUid = StorageSaved.WD_UID
        });

        return db;
    }
}

/// <summary>
/// Widget shown on a dashboard
/// </summary>
public class Widget
{
    /// <summary>
    /// Gets or sets the width of the widget
    /// </summary>
    public int Width { get; set; }
    /// <summary>
    /// Gets or sets the height of the widget
    /// </summary>
    public int Height { get; set; }
    /// <summary>
    /// Gets or sets the X position of the widget
    /// </summary>
    public int X { get; set; }
    /// <summary>
    /// Gets or sets the Y position of the widget
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// The UID of the Widget Definition
    /// </summary>
    public Guid WidgetDefinitionUid { get; set; }
}