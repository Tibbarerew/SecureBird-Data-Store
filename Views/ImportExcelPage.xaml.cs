using SecureBird_Data_Store.ViewModels;

namespace SecureBird_Data_Store.Views;

public partial class ImportExcelPage : ContentPage
{
    public ImportExcelPage(ImportExcelViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
