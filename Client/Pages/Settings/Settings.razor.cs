using System.Text;

namespace FileFlows.Client.Pages;

using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Helpers;
using FileFlows.Shared.Helpers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using FileFlows.Shared;
using FileFlows.Client.Components;
using System.Collections.Generic;
using FileFlows.Shared.Validators;
using Microsoft.JSInterop;
using FileFlows.Plugin;

/// <summary>
/// Page for system settings
/// </summary>
public partial class Settings : InputRegister
{
    /// <summary>
    /// Gets or sets blocker instance
    /// </summary>
    [CascadingParameter] Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the javascript runtime used
    /// </summary>
    [Inject] IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation manager used
    /// </summary>
    [Inject] private NavigationManager  NavigationManager { get; set; }

    private bool ShowExternalDatabase => LicensedFor(LicenseFlags.ExternalDatabase);

    private bool IsSaving { get; set; }

    private string lblSave, lblSaving, lblHelp, lblGeneral, lblAdvanced, lblNode, lblDatabase, lblLogging, 
        lblInternalProcessingNodeDescription, lblDbDescription, lblFileServerDescription, lblTest, lblRestart, lblLicense, lblUpdates, 
        lblCheckNow, lblTestingDatabase, lblFileServer;

    private string OriginalDatabase, OriginalServer;

    private SettingsUiModel Model { get; set; } = new ();
    private string LicenseFlagsString = string.Empty;
    // indicates if the page has rendered or not
    private DateTime firstRenderedAt = DateTime.MaxValue;
    
    private ProcessingNode InternalProcessingNode { get; set; }

    private readonly List<Validator> RequiredValidator = new()
    {
        new Required()
    };

    private readonly List<ListOption> DbTypes = new()
    {
        new() { Label = "SQLite", Value = DatabaseType.Sqlite },
        //new() { Label = "SQL Server", Value = DatabaseType.SqlServer }, // not finished yet
        new() { Label = "MySQL", Value = DatabaseType.MySql }
    };

    /// <summary>
    /// The languages available in the system
    /// </summary>
    private readonly List<ListOption> LanguageOptions = new()
    {
        new() { Label = "English", Value = "en" },
        new() { Label = "Deutsch", Value = "de" },
    };

    /// <summary>
    /// Gets or sets the type of databsae to use
    /// </summary>
    private object DbType
    {
        get => Model.DbType;
        set
        {
            if (value is DatabaseType dbType)
            {
                Model.DbType = dbType;
                if (dbType != DatabaseType.Sqlite && string.IsNullOrWhiteSpace(Model.DbName))
                    Model.DbName = "FileFlows";
            }
        }
    }

    /// <summary>
    /// Gets or sets the language
    /// </summary>
    private object Language
    {
        get => Model.Language;
        set
        {
            if (value is string lang)
            {
                Model.Language = lang;
            }
        }
    }

    private string initialLannguage;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        lblSave = Translater.Instant("Labels.Save");
        lblSaving = Translater.Instant("Labels.Saving");
        lblHelp = Translater.Instant("Labels.Help");
        lblAdvanced = Translater.Instant("Labels.Advanced");
        lblLicense = Translater.Instant("Labels.License");
        lblGeneral = Translater.Instant("Pages.Settings.Labels.General");
        lblNode = Translater.Instant("Pages.Settings.Labels.InternalProcessingNode");
        lblUpdates = Translater.Instant("Pages.Settings.Labels.Updates");
        lblDatabase = Translater.Instant("Pages.Settings.Labels.Database");
        lblInternalProcessingNodeDescription = Translater.Instant("Pages.Settings.Fields.InternalProcessingNode.Description");
        lblDbDescription = Translater.Instant("Pages.Settings.Fields.Database.Description");
        lblTest = Translater.Instant("Labels.Test");
        lblRestart= Translater.Instant("Pages.Settings.Labels.Restart");
        lblLogging= Translater.Instant("Pages.Settings.Labels.Logging");
        lblCheckNow = Translater.Instant("Pages.Settings.Labels.CheckNow");
        lblTestingDatabase = Translater.Instant("Pages.Settings.Messages.Database.TestingDatabase");
        lblFileServer = Translater.Instant("Pages.Settings.Labels.FileServer");
        lblFileServerDescription = Translater.Instant("Pages.Settings.Fields.FileServer.Description");
        Blocker.Show("Loading Settings");
        try
        {
            await Refresh();
            initialLannguage = Model.Language;
        }
        finally
        {
            Blocker.Hide();
        }
    }


    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
            firstRenderedAt = DateTime.UtcNow;
        base.OnAfterRender(firstRender);
    }

    
    private async Task Refresh(bool blocker = true)
    {
        if(blocker)
            Blocker.Show();
#if (!DEMO)
        var response = await HttpHelper.Get<SettingsUiModel>("/api/settings/ui-settings");
        if (response.Success)
        {
            this.Model = response.Data;
            LicenseFlagsString = LicenseFlagsToString(Model.LicenseFlags);
            this.OriginalServer = this.Model?.DbServer;
            this.OriginalDatabase = this.Model?.DbName;
            if (this.Model != null && this.Model.DbPort < 1)
                this.Model.DbPort = 3306;
        }

        var nodesResponse = await HttpHelper.Get<ProcessingNode[]>("/api/node");
        if (nodesResponse.Success)
        {
            this.InternalProcessingNode = nodesResponse.Data.FirstOrDefault(x => x.Address == "FileFlowsServer") ?? new ();
        }
        this.StateHasChanged();
#endif
        if(blocker)
            Blocker.Hide();
    }

    private async Task Save()
    {
#if (DEMO)
        return;
#else
        this.Blocker.Show(lblSaving);
        this.IsSaving = true;
        try
        {

            bool valid = await this.Validate();
            if (valid == false)
                return;
            
            await HttpHelper.Put<string>("/api/settings/ui-settings", this.Model);

            if (this.InternalProcessingNode != null)
            {
                await HttpHelper.Post("/api/node", this.InternalProcessingNode);
            }

            await App.Instance.LoadAppInfo();

            if (initialLannguage != Model.Language)
            {
                // need to do a full page reload
                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
            else
            {
                await this.Refresh();
            }
        }
        finally
        {
            this.IsSaving = false;
            this.Blocker.Hide();
        }
#endif
    }

    private void OpenHelp()
    {
        App.Instance.OpenHelp("https://fileflows.com/docs/webconsole/settings");
    }

    private async Task TestDbConnection()
    {
        string server = Model?.DbServer?.Trim();
        string name = Model?.DbName?.Trim();
        string user = Model?.DbUser?.Trim();
        string password = Model?.DbPassword?.Trim();
        int port = Model?.DbPort ?? 0;
        if (string.IsNullOrWhiteSpace(server))
        {
            Toast.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoServer"));
            return;
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            Toast.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoName"));
            return;
        }
        if (string.IsNullOrWhiteSpace(user))
        {
            Toast.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoUser"));
            return;
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            Toast.ShowError(Translater.Instant("Pages.Settings.Messages.Database.NoPassword"));
            return;
        }

        Blocker.Show(lblTestingDatabase);
        try
        {
            var result = await HttpHelper.Post<string>("/api/settings/test-db-connection", new
            {
                server, name, port, user, password, Type = DbType
            });
            if (result.Success == false)
                throw new Exception(result.Body);
            if(result.Data != "OK")
                throw new Exception(result.Data);
            Toast.ShowSuccess(Translater.Instant("Pages.Settings.Messages.Database.TestSuccess"));
        }
        catch (Exception ex)
        {
            Toast.ShowError(ex.Message);
        }
        finally
        {
            Blocker.Hide();
        }
    }

    async void Restart()
    {
        var confirmed = await Confirm.Show(
            Translater.Instant("Pages.Settings.Messages.Restart.Title"),
            Translater.Instant("Pages.Settings.Messages.Restart.Message")
        );
        if (confirmed == false)
            return;
        await Save();
        await HttpHelper.Post("/api/system/restart");
    }

    private bool IsLicensed => string.IsNullOrEmpty(Model?.LicenseStatus) == false && Model.LicenseStatus != "Unlicensed" && Model.LicenseStatus != "Invalid";

    /// <summary>
    /// Checks if the user is licensed for a feature
    /// </summary>
    /// <param name="feature">the feature to check</param>
    /// <returns>If the user is licensed for a feature</returns>
    private bool LicensedFor(LicenseFlags feature)
    {
        if (IsLicensed == false)
            return false;
        return (Model.LicenseFlags & feature) == feature;
    }

    private async Task CheckForUpdateNow()
    {
        try
        {
            await HttpHelper.Post("/api/settings/check-for-update-now");
            Toast.ShowSuccess("Pages.Settings.Messages.CheckUpdateSuccess");
        }
        catch (Exception)
        {
            Toast.ShowError("Pages.Settings.Messages.CheckUpdateFailed");
        }
    }
    
    /// <summary>
    /// Enumerates through the specified enum flags and returns a comma-separated string
    /// containing the names of the enum values that are present in the given flags.
    /// </summary>
    /// <param name="myValue">The enum value with flags set.</param>
    /// <returns>A comma-separated string of the enum values present in the given flags.</returns>
    string LicenseFlagsToString(LicenseFlags myValue)
    {
        string myString = "";

        foreach (LicenseFlags enumValue in Enum.GetValues(typeof(LicenseFlags)))
        {
            if (enumValue == LicenseFlags.NotLicensed)
                continue;
            if (myValue.HasFlag(enumValue))
            {
                myString += SplitWordsOnCapitalLetters(enumValue.ToString()) + "\n";
            }
        }

        // Remove the trailing comma if any
        if (!string.IsNullOrEmpty(myString))
        {
            myString = myString.TrimEnd('\n');
        }

        return myString;
    }
    
    /// <summary>
    /// Splits a given input string into separate words whenever a capital letter is encountered.
    /// </summary>
    /// <param name="input">The input string to be split.</param>
    /// <returns>A new string with spaces inserted before each capital letter (except the first one).</returns>
    string SplitWordsOnCapitalLetters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        StringBuilder sb = new StringBuilder();
        foreach (char c in input)
        {
            if (char.IsUpper(c) && sb.Length > 0)
                sb.Append(' ');
            sb.Append(c);
        }

        return sb.ToString();
    }
    
    
    private async Task OnTelemetryChange(bool disabled)
    {
        if (firstRenderedAt < DateTime.UtcNow.AddSeconds(-1) && disabled)
        {
            if (await Confirm.Show("Pages.Settings.Messages.DisableTelemetryConfirm.Title",
                    "Pages.Settings.Messages.DisableTelemetryConfirm.Message",
                    false) == false)
            {
                Model.DisableTelemetry = false;
            }
        }
    }
}
