using System.Threading;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Services;

/// <summary>
/// Profile service
/// </summary>
public class ProfileService
{
    /// <summary>
    /// Semaphore to ensure profile is only fetched once
    /// </summary>
    private SemaphoreSlim _semaphore = new(1);
    /// <summary>
    /// The cached profile
    /// </summary>
    private Profile _profile;

    private NavigationManager NavigationManager;

    public ProfileService(NavigationManager navigationManager)
    {
        NavigationManager = navigationManager;
    }

    /// <summary>
    /// Gets the profile
    /// </summary>
    /// <returns>the users profile</returns>
    public async Task<Profile> Get()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_profile != null)
                return _profile;
            var result = await HttpHelper.Get<Profile>("/api/profile");
            if (result.Success == false)
            {
                NavigationManager.NavigateTo("/login", true);
                return null;
            }

            _profile = result.Data;
            return _profile;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Refreshes the profile
    /// </summary>
    public async Task Refresh()
    {
        await _semaphore.WaitAsync();
        try
        {
            var result = await HttpHelper.Get<Profile>("/api/profile");
            if (result.Success == false)
            {
                NavigationManager.NavigateTo("/login");
                return;
            }
            var newProfile = result.Data;
            if (_profile == null)
            {
                _profile = newProfile;
                return;
            }

            _profile.ConfigurationStatus = newProfile.ConfigurationStatus;
            _profile.Uid = newProfile.Uid;
            _profile.Name = newProfile.Name;
            _profile.Language = newProfile.Language;
            _profile.License = newProfile.License;
            _profile.Role = newProfile.Role;
            _profile.Security = newProfile.Security;
            _profile.IsWebView = newProfile.IsWebView;
        }
        finally
        {
            
        }
    }
}