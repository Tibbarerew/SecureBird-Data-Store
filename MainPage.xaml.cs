using SecureBird_Data_Store.Views;

namespace SecureBird_Data_Store;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnManageStructuresClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(DataStructuresPage));
    }

    private async void OnImportExcelClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ImportExcelPage));
    }

    private async void OnHierarchyClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(HierarchyPage));
    }

    private async void OnProfilesClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ImportProfilesPage));
    }

    private async void OnExportClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ExportPage));
    }
}
