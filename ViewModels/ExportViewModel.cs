using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureBird_Data_Store.Models;
using SecureBird_Data_Store.Services;

namespace SecureBird_Data_Store.ViewModels;

public partial class ExportViewModel : BaseViewModel
{
    private readonly IJsonDataService _dataService;
    private readonly IExportService _exportService;
    private List<DataRecord> _loadedRecords = [];

    [ObservableProperty]
    private ObservableCollection<DataStructure> _structures = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFieldPickers))]
    private DataStructure? _selectedStructure;

    [ObservableProperty]
    private ObservableCollection<FieldDefinition> _availableFields = [];

    [ObservableProperty]
    private FieldDefinition? _promptField;

    [ObservableProperty]
    private FieldDefinition? _completionField;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFieldPickers))]
    private int _selectedFormatIndex;

    [ObservableProperty]
    private string _preview = string.Empty;

    [ObservableProperty]
    private int _recordCount;

    [ObservableProperty]
    private string _exportedPath = string.Empty;

    public bool ShowFieldPickers => SelectedFormatIndex is 2 or 3;

    public List<string> FormatNames { get; } = [
        "JSONL — standard (all fields)",
        "CSV — spreadsheet-compatible",
        "Fine-tuning: messages (OpenAI chat format)",
        "Fine-tuning: prompt / completion (simple)"
    ];

    public ExportViewModel(IJsonDataService dataService, IExportService exportService)
    {
        _dataService = dataService;
        _exportService = exportService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            var list = await _dataService.GetStructuresAsync();
            Structures = new ObservableCollection<DataStructure>(list);
        }, "Loading...");
    }

    partial void OnSelectedStructureChanged(DataStructure? value)
        => _ = LoadRecordsAsync(value);

    partial void OnSelectedFormatIndexChanged(int value) => RefreshPreview();
    partial void OnPromptFieldChanged(FieldDefinition? value) => RefreshPreview();
    partial void OnCompletionFieldChanged(FieldDefinition? value) => RefreshPreview();

    private async Task LoadRecordsAsync(DataStructure? structure)
    {
        ExportedPath = string.Empty;
        if (structure is null)
        {
            _loadedRecords = [];
            AvailableFields = [];
            RecordCount = 0;
            Preview = string.Empty;
            return;
        }

        _loadedRecords = await _dataService.GetRecordsAsync(structure.Id);
        RecordCount = _loadedRecords.Count;
        AvailableFields = new ObservableCollection<FieldDefinition>(structure.Fields);

        PromptField = structure.PromptFieldName is not null
            ? structure.Fields.FirstOrDefault(f => f.Name == structure.PromptFieldName)
            : null;
        CompletionField = structure.CompletionFieldName is not null
            ? structure.Fields.FirstOrDefault(f => f.Name == structure.CompletionFieldName)
            : null;

        RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (SelectedStructure is null || _loadedRecords.Count == 0)
        {
            Preview = _loadedRecords.Count == 0 && SelectedStructure is not null
                ? "(no records in this structure)"
                : string.Empty;
            return;
        }

        var sample = _loadedRecords.Take(3).ToList();
        var format = IndexToFormat(SelectedFormatIndex);
        Preview = _exportService.BuildContent(SelectedStructure, sample, format,
            PromptField?.Name, CompletionField?.Name);
    }

    [RelayCommand]
    public async Task ExportAsync()
    {
        if (SelectedStructure is null) { StatusMessage = "Select a structure first."; return; }
        if (_loadedRecords.Count == 0) { StatusMessage = "No records to export."; return; }

        await RunAsync(async () =>
        {
            var format = IndexToFormat(SelectedFormatIndex);
            var content = _exportService.BuildContent(SelectedStructure, _loadedRecords, format,
                PromptField?.Name, CompletionField?.Name);
            ExportedPath = await _exportService.SaveExportAsync(content, SelectedStructure.Name, format);
            StatusMessage = $"Exported {_loadedRecords.Count} records.";
        }, "Exporting...");
    }

    [RelayCommand]
    public void OpenExportFolder()
    {
        var folder = string.IsNullOrEmpty(ExportedPath)
            ? _exportService.ExportFolder
            : Path.GetDirectoryName(ExportedPath) ?? _exportService.ExportFolder;

        Directory.CreateDirectory(folder);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", $"\"{folder}\"")
        {
            UseShellExecute = true
        });
    }

    private static ExportFormat IndexToFormat(int index) => index switch
    {
        1 => ExportFormat.Csv,
        2 => ExportFormat.FineTuningMessages,
        3 => ExportFormat.FineTuningSimple,
        _ => ExportFormat.Jsonl
    };
}
