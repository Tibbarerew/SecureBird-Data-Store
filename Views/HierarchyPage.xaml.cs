using SecureBird_Data_Store.ViewModels;

namespace SecureBird_Data_Store.Views;

public partial class HierarchyPage : ContentPage
{
    private readonly HierarchyViewModel _vm;

    public HierarchyPage(HierarchyViewModel vm)
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
