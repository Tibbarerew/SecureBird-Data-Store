using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureBird_Data_Store.Models;
using SecureBird_Data_Store.Services;

namespace SecureBird_Data_Store.ViewModels;

public partial class ImportProfilesViewModel : BaseViewModel
{
    private readonly IProfileService _profileService;

    [ObservableProperty]
    private ObservableCollection<ImportProfile> _profiles = [];

    [ObservableProperty]
    private ImportProfile? _selectedProfile;

    public ImportProfilesViewModel(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            var list = await _profileService.GetProfilesAsync();
            Profiles = new ObservableCollection<ImportProfile>(list);
        }, "Loading profiles...");
    }

    [RelayCommand]
    public async Task DeleteAsync(ImportProfile profile)
    {
        await RunAsync(async () =>
        {
            await _profileService.DeleteProfileAsync(profile.Id);
            await LoadAsync();
        }, "Deleting...");
    }

    [RelayCommand]
    public async Task RenameAsync(ImportProfile profile)
    {
        var name = await Shell.Current.DisplayPromptAsync(
            "Rename Profile", "Enter new name:", initialValue: profile.Name);

        if (string.IsNullOrWhiteSpace(name)) return;

        profile.Name = name.Trim();
        await _profileService.SaveProfileAsync(profile);
        await LoadAsync();
    }

    [RelayCommand]
    public void SelectProfile(ImportProfile profile)
        => SelectedProfile = SelectedProfile?.Id == profile.Id ? null : profile;
}
