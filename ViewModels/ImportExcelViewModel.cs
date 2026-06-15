using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureBird_Data_Store.Models;
using SecureBird_Data_Store.Services;

namespace SecureBird_Data_Store.ViewModels;

public partial class ImportExcelViewModel : BaseViewModel
{
    private readonly IExcelImportService _excelService;
    private readonly IJsonDataService _dataService;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SheetImportConfig> _sheetConfigs = [];

    [ObservableProperty]
    private ObservableCollection<DataStructure> _existingStructures = [];

    [ObservableProperty]
    private bool _importComplete;

    [ObservableProperty]
    private int _importedRecords;

    [ObservableProperty]
    private int _importedStructures;

    public ImportExcelViewModel(IExcelImportService excelService, IJsonDataService dataService)
    {
        _excelService = excelService;
        _dataService = dataService;
    }

    [RelayCommand]
    public async Task PickFileAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select an Excel file",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, [".xlsx", ".xls"] },
                { DevicePlatform.macOS, ["xlsx", "xls"] }
            })
        });

        if (result is null) return;

        FilePath = result.FullPath;
        FileName = result.FileName;

        await RunAsync(async () =>
        {
            var importResult = await _excelService.ReadExcelAsync(FilePath);
            if (!importResult.Success)
            {
                StatusMessage = $"Could not read file: {importResult.ErrorMessage}";
                return;
            }

            var structures = await _dataService.GetStructuresAsync();
            ExistingStructures = new ObservableCollection<DataStructure>(structures);

            // Build a config for each sheet
            var configs = new ObservableCollection<SheetImportConfig>();
            foreach (var sheet in importResult.Sheets)
            {
                var config = new SheetImportConfig
                {
                    Sheet = sheet,
                    StructureName = sheet.SheetName,
                    CreateNew = true,
                    Include = true
                };
                config.PropertyChanged += OnConfigPropertyChanged;
                configs.Add(config);
            }
            SheetConfigs = configs;
        }, "Reading Excel file...");
    }

    private async void OnConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not SheetImportConfig config) return;

        if (e.PropertyName == nameof(SheetImportConfig.ParentStructure))
        {
            if (config.ParentStructure is null)
            {
                config.SetAvailableParentRecords([], null);
                return;
            }
            var records = await _dataService.GetRecordsAsync(config.ParentStructure.Id);
            config.SetAvailableParentRecords(records, config.ParentStructure);
        }
    }

    [RelayCommand]
    public async Task ImportAsync()
    {
        var included = SheetConfigs.Where(c => c.Include).ToList();
        if (included.Count == 0)
        {
            StatusMessage = "No sheets selected for import.";
            return;
        }

        await RunAsync(async () =>
        {
            int totalRecords = 0;
            int totalStructures = 0;

            foreach (var config in included)
            {
                DataStructure structure;

                if (config.CreateNew)
                {
                    var name = string.IsNullOrWhiteSpace(config.StructureName)
                        ? config.SheetName
                        : config.StructureName;
                    structure = _excelService.SuggestStructure(config.Sheet, name);

                    if (config.HasParent && config.SelectedParentRecord is not null)
                        structure.ParentStructureId = config.SelectedParentRecord.StructureId;

                    await _dataService.SaveStructureAsync(structure);
                    totalStructures++;
                }
                else
                {
                    if (config.ExistingStructure is null) continue;
                    structure = config.ExistingStructure;
                }

                var parentRecordId = config.HasParent
                    ? config.SelectedParentRecord?.Id
                    : null;

                var records = await _excelService.ImportSheetAsRecordsAsync(
                    FilePath, config.SheetName, structure.Id, config.Sheet.Headers);

                foreach (var record in records)
                {
                    record.ParentRecordId = parentRecordId;
                    await _dataService.SaveRecordAsync(record);
                    totalRecords++;
                }
            }

            ImportedRecords = totalRecords;
            ImportedStructures = totalStructures;
            ImportComplete = true;
        }, "Importing...");
    }

    [RelayCommand]
    public void Reset()
    {
        foreach (var config in SheetConfigs)
            config.PropertyChanged -= OnConfigPropertyChanged;

        FilePath = string.Empty;
        FileName = string.Empty;
        SheetConfigs = [];
        ImportComplete = false;
        ImportedRecords = 0;
        ImportedStructures = 0;
        StatusMessage = string.Empty;
    }
}
