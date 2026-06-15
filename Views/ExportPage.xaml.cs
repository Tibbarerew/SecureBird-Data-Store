using SecureBird_Data_Store.ViewModels;

namespace SecureBird_Data_Store.Views;

public partial class ExportPage : ContentPage
{
    private readonly ExportViewModel _vm;

    public ExportPage(ExportViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
