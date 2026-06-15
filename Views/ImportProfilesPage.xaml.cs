using SecureBird_Data_Store.ViewModels;

namespace SecureBird_Data_Store.Views;

public partial class ImportProfilesPage : ContentPage
{
    private readonly ImportProfilesViewModel _vm;

    public ImportProfilesPage(ImportProfilesViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
