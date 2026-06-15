using SecureBird_Data_Store.Views;

namespace SecureBird_Data_Store;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(DataStructuresPage), typeof(DataStructuresPage));
        Routing.RegisterRoute(nameof(DataRecordsPage), typeof(DataRecordsPage));
        Routing.RegisterRoute(nameof(ImportExcelPage), typeof(ImportExcelPage));
        Routing.RegisterRoute(nameof(HierarchyPage), typeof(HierarchyPage));
        Routing.RegisterRoute(nameof(ImportProfilesPage), typeof(ImportProfilesPage));
    }
}
