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
    private readonly IProfileService _profileService;

    // Maps sheet name → structure ID for structures created during this import session
    private readonly Dictionary<string, string> _sessionStructureIds = [];

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SheetImportConfig> _sheetConfigs = [];

    [ObservableProperty]
    private ObservableCollection<DataStructure> _existingStructures = [];

    [ObservableProperty]
    private ObservableCollection<ImportProfile> _availableProfiles = [];

    [ObservableProperty]
    private ImportProfile? _loadedProfile;

    [ObservableProperty]
    private bool _importComplete;

    [ObservableProperty]
    private int _importedRecords;

    [ObservableProperty]
    private int _importedStructures;

    public ImportExcelViewModel(
        IExcelImportService excelService,
        IJsonDataService dataService,
        IProfileService profileService)
    {
        _excelService = excelService;
        _dataService = dataService;
        _profileService = profileService;
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

            var profiles = await _profileService.GetProfilesAsync();
            AvailableProfiles = new ObservableCollection<ImportProfile>(profiles);

            foreach (var config in SheetConfigs)
                config.PropertyChanged -= OnConfigPropertyChanged;

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

            // Auto-match sheets to existing structures by name (case-insensitive)
            foreach (var config in SheetConfigs)
            {
                var match = ExistingStructures.FirstOrDefault(
                    s => s.Name.Equals(config.SheetName, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                {
                    config.CreateNew = false;
                    config.ExistingStructure = match;
                }
            }
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

        // Option 3: when an existing structure is selected, auto-apply its default parent rule
        if (e.PropertyName == nameof(SheetImportConfig.ExistingStructure))
        {
            await ApplyStructureDefaultParentAsync(config, config.ExistingStructure);
        }
    }

    private async Task ApplyStructureDefaultParentAsync(SheetImportConfig config, DataStructure? structure)
    {
        if (structure?.DefaultParentRecordId is null || structure.ParentStructureId is null)
            return;

        var parentStructure = ExistingStructures.FirstOrDefault(s => s.Id == structure.ParentStructureId);
        if (parentStructure is null) return;

        var records = await _dataService.GetRecordsAsync(parentStructure.Id);
        var defaultParent = records.FirstOrDefault(r => r.Id == structure.DefaultParentRecordId);
        if (defaultParent is null) return;

        config.HasParent = true;
        config.ParentStructure = parentStructure;
        config.SetAvailableParentRecords(records, parentStructure);
        config.SelectedParentRecord = defaultParent;
    }

    [RelayCommand]
    public async Task ApplyProfileAsync(ImportProfile profile)
    {
        if (SheetConfigs.Count == 0) return;
        LoadedProfile = profile;

        await RunAsync(async () =>
        {
            foreach (var config in SheetConfigs)
            {
                var entry = profile.Entries.FirstOrDefault(
                    e => e.SheetName.Equals(config.SheetName, StringComparison.OrdinalIgnoreCase));
                if (entry is null) continue;

                config.Include = entry.Include;
                config.CreateNew = entry.CreateNew;
                config.StructureName = entry.StructureName;

                if (!entry.CreateNew && entry.StructureId is not null)
                {
                    config.ExistingStructure = ExistingStructures.FirstOrDefault(s => s.Id == entry.StructureId);
                    // Option 3: auto-apply structural default parent from structure definition
                    await ApplyStructureDefaultParentAsync(config, config.ExistingStructure);
                }

                // Profile-stored parent overrides structural default (it's more specific)
                if (entry.ParentRecordId is not null && entry.ParentStructureId is not null)
                {
                    var parentStructure = ExistingStructures.FirstOrDefault(s => s.Id == entry.ParentStructureId);
                    if (parentStructure is not null)
                    {
                        var records = await _dataService.GetRecordsAsync(parentStructure.Id);
                        var parentRecord = records.FirstOrDefault(r => r.Id == entry.ParentRecordId);
                        if (parentRecord is not null)
                        {
                            config.HasParent = true;
                            config.ParentStructure = parentStructure;
                            config.SetAvailableParentRecords(records, parentStructure);
                            config.SelectedParentRecord = parentRecord;
                        }
                    }
                }
            }
        }, "Applying profile...");
    }

    [RelayCommand]
    public async Task SaveProfileAsync()
    {
        if (SheetConfigs.Count == 0) return;

        var name = await Shell.Current.DisplayPromptAsync(
            "Save Profile",
            "Enter a name for this import profile:",
            initialValue: LoadedProfile?.Name ?? Path.GetFileNameWithoutExtension(FileName));

        if (string.IsNullOrWhiteSpace(name)) return;

        var profile = new ImportProfile
        {
            Id = LoadedProfile?.Id ?? Guid.NewGuid().ToString(),
            Name = name.Trim(),
            SourceFileName = FileName,
            Entries = SheetConfigs.Select(c => new SheetProfileEntry
            {
                SheetName = c.SheetName,
                Include = c.Include,
                CreateNew = c.CreateNew,
                StructureName = c.StructureName,
                StructureId = c.CreateNew
                    ? _sessionStructureIds.GetValueOrDefault(c.SheetName)
                    : c.ExistingStructure?.Id,
                ParentStructureId = c.HasParent ? c.ParentStructure?.Id : null,
                ParentRecordId = c.HasParent ? c.SelectedParentRecord?.Id : null
            }).ToList()
        };

        await _profileService.SaveProfileAsync(profile);
        LoadedProfile = profile;

        var profiles = await _profileService.GetProfilesAsync();
        AvailableProfiles = new ObservableCollection<ImportProfile>(profiles);

        StatusMessage = $"Profile \"{name}\" saved.";
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

        _sessionStructureIds.Clear();

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
                        ? config.SheetName : config.StructureName;
                    structure = _excelService.SuggestStructure(config.Sheet, name);

                    if (config.HasParent && config.SelectedParentRecord is not null)
                    {
                        structure.ParentStructureId = config.ParentStructure?.Id;
                        structure.DefaultParentRecordId = config.SelectedParentRecord.Id;
                    }

                    await _dataService.SaveStructureAsync(structure);
                    _sessionStructureIds[config.SheetName] = structure.Id;
                    totalStructures++;
                }
                else
                {
                    if (config.ExistingStructure is null) continue;
                    structure = config.ExistingStructure;
                }

                var parentRecordId = config.HasParent ? config.SelectedParentRecord?.Id : null;

                // Fall back to structure's default parent rule if no explicit parent chosen
                if (parentRecordId is null && structure.DefaultParentRecordId is not null)
                    parentRecordId = structure.DefaultParentRecordId;

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
        LoadedProfile = null;
        ImportComplete = false;
        ImportedRecords = 0;
        ImportedStructures = 0;
        StatusMessage = string.Empty;
        _sessionStructureIds.Clear();
    }
}
