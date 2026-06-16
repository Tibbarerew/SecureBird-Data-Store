using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureBird_Data_Store.Models;
using SecureBird_Data_Store.Services;

namespace SecureBird_Data_Store.ViewModels;

public class FieldEntry
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public FieldDefinition? Definition { get; set; }
}

public partial class DataRecordsViewModel : BaseViewModel
{
    private readonly IJsonDataService _dataService;

    [ObservableProperty]
    private DataStructure? _currentStructure;

    [ObservableProperty]
    private ObservableCollection<DataRecord> _records = [];

    [ObservableProperty]
    private DataRecord? _selectedRecord;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private ObservableCollection<FieldEntry> _editFields = [];

    public DataRecordsViewModel(IJsonDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task LoadForStructureAsync(DataStructure structure)
    {
        CurrentStructure = structure;
        await RunAsync(async () =>
        {
            var list = await _dataService.GetRecordsAsync(structure.Id);
            Records = new ObservableCollection<DataRecord>(list);
        }, "Loading records...");
    }

    [RelayCommand]
    public void NewRecord()
    {
        if (CurrentStructure is null) return;
        SelectedRecord = null;
        EditFields = new ObservableCollection<FieldEntry>(
            CurrentStructure.Fields.Select(f => new FieldEntry
            {
                Key = f.Name,
                Value = f.DefaultValue,
                Definition = f
            }));
        IsEditing = true;
    }

    [RelayCommand]
    public void EditRecord(DataRecord record)
    {
        if (CurrentStructure is null) return;
        SelectedRecord = record;
        EditFields = new ObservableCollection<FieldEntry>(
            CurrentStructure.Fields.Select(f => new FieldEntry
            {
                Key = f.Name,
                Value = record.Fields.TryGetValue(f.Name, out var v) ? v : null,
                Definition = f
            }));
        IsEditing = true;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (CurrentStructure is null) return;

        await RunAsync(async () =>
        {
            var record = SelectedRecord ?? new DataRecord { StructureId = CurrentStructure.Id };
            record.Fields = EditFields.ToDictionary(f => f.Key, f => f.Value);

            await _dataService.SaveRecordAsync(record);
            IsEditing = false;
            await LoadForStructureAsync(CurrentStructure);
        }, "Saving...");
    }

    [RelayCommand]
    public void Cancel()
    {
        IsEditing = false;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    public async Task DeleteAsync(DataRecord record)
    {
        if (CurrentStructure is null) return;

        var confirmed = await Shell.Current.DisplayAlert(
            "Delete Record",
            $"Delete \"{record.DisplayName}\"? This cannot be undone.",
            "Delete", "Cancel");

        if (!confirmed) return;

        await RunAsync(async () =>
        {
            await _dataService.DeleteRecordAsync(CurrentStructure.Id, record.Id);
            await LoadForStructureAsync(CurrentStructure);
        }, "Deleting...");
    }
}
