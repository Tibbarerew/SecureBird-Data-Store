using SecureBird_Data_Store.Models;
using SecureBird_Data_Store.ViewModels;

namespace SecureBird_Data_Store.Views;

[QueryProperty(nameof(Structure), "Structure")]
public partial class DataRecordsPage : ContentPage
{
    private readonly DataRecordsViewModel _vm;

    public DataStructure? Structure
    {
        set
        {
            if (value is not null)
                _ = _vm.LoadForStructureAsync(value);
        }
    }

    public DataRecordsPage(DataRecordsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }
}
