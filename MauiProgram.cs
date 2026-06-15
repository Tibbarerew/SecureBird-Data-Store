using Microsoft.Extensions.Logging;
using SecureBird_Data_Store.Services;
using SecureBird_Data_Store.ViewModels;
using SecureBird_Data_Store.Views;

namespace SecureBird_Data_Store;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<IJsonDataService, JsonDataService>();
        builder.Services.AddSingleton<IExcelImportService, ExcelImportService>();

        // ViewModels
        builder.Services.AddTransient<DataStructuresViewModel>();
        builder.Services.AddTransient<DataRecordsViewModel>();
        builder.Services.AddTransient<ImportExcelViewModel>();
        builder.Services.AddTransient<HierarchyViewModel>();

        // Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<DataStructuresPage>();
        builder.Services.AddTransient<DataRecordsPage>();
        builder.Services.AddTransient<ImportExcelPage>();
        builder.Services.AddTransient<HierarchyPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
