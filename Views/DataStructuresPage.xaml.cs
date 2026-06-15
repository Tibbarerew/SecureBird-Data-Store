using SecureBird_Data_Store.ViewModels;

namespace SecureBird_Data_Store.Views;

public partial class DataStructuresPage : ContentPage
{
    private readonly DataStructuresViewModel _vm;

    public DataStructuresPage(DataStructuresViewModel vm)
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
