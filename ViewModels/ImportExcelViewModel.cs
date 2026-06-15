using System.Collections.ObjectModel;
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
    private ExcelImportResult? _importResult;

    [ObservableProperty]
    private ExcelSheetPreview? _selectedSheet;

    [ObservableProperty]
    private ObservableCollection<DataStructure> _existingStructures = [];

    [ObservableProperty]
    private DataStructure? _targetStructure;

    [ObservableProperty]
    private string _newStructureName = string.Empty;

    [ObservableProperty]
    private bool _createNewStructure = true;

    [ObservableProperty]
    private bool _importComplete;

    [ObservableProperty]
    private int _importedCount;

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
        await RunAsync(async () =>
        {
            ImportResult = await _excelService.ReadExcelAsync(FilePath);
            if (ImportResult.Success && ImportResult.Sheets.Count > 0)
            {
                SelectedSheet = ImportResult.Sheets[0];
                NewStructureName = Path.GetFileNameWithoutExtension(FilePath);
            }
        }, "Reading Excel file...");

        var structures = await _dataService.GetStructuresAsync();
        ExistingStructures = new ObservableCollection<DataStructure>(structures);
    }

    [RelayCommand]
    public async Task ImportAsync()
    {
        if (SelectedSheet is null || string.IsNullOrWhiteSpace(FilePath))
        {
            StatusMessage = "Please select a file and sheet.";
            return;
        }

        await RunAsync(async () =>
        {
            DataStructure structure;

            if (CreateNewStructure)
            {
                var name = string.IsNullOrWhiteSpace(NewStructureName)
                    ? SelectedSheet.SheetName
                    : NewStructureName;
                structure = _excelService.SuggestStructure(SelectedSheet, name);
                await _dataService.SaveStructureAsync(structure);
            }
            else
            {
                if (TargetStructure is null)
                {
                    StatusMessage = "Please select an existing structure.";
                    return;
                }
                structure = TargetStructure;
            }

            var records = await _excelService.ImportSheetAsRecordsAsync(
                FilePath, SelectedSheet.SheetName, structure.Id, SelectedSheet.Headers);

            foreach (var record in records)
                await _dataService.SaveRecordAsync(record);

            ImportedCount = records.Count;
            ImportComplete = true;
        }, "Importing data...");
    }

    [RelayCommand]
    public void Reset()
    {
        FilePath = string.Empty;
        ImportResult = null;
        SelectedSheet = null;
        ImportComplete = false;
        ImportedCount = 0;
        StatusMessage = string.Empty;
        NewStructureName = string.Empty;
        CreateNewStructure = true;
    }
}
