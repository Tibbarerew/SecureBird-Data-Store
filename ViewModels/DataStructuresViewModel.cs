using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureBird_Data_Store.Models;
using SecureBird_Data_Store.Services;

namespace SecureBird_Data_Store.ViewModels;

public partial class DataStructuresViewModel : BaseViewModel
{
    private readonly IJsonDataService _dataService;

    [ObservableProperty]
    private ObservableCollection<DataStructure> _structures = [];

    [ObservableProperty]
    private DataStructure? _selectedStructure;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private ObservableCollection<FieldDefinition> _editFields = [];

    // Default parent rule (Option 3)
    [ObservableProperty]
    private bool _hasDefaultParent;

    [ObservableProperty]
    private DataStructure? _defaultParentStructure;

    [ObservableProperty]
    private ObservableCollection<DataRecord> _defaultParentRecords = [];

    [ObservableProperty]
    private DataRecord? _defaultParentRecord;

    [ObservableProperty]
    private string _defaultParentLabel = string.Empty;

    // Prompt/completion field mapping for export
    [ObservableProperty]
    private FieldDefinition? _editPromptField;

    [ObservableProperty]
    private FieldDefinition? _editCompletionField;

    public DataStructuresViewModel(IJsonDataService dataService)
    {
        _dataService = dataService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            var list = await _dataService.GetStructuresAsync();
            Structures = new ObservableCollection<DataStructure>(list);
        }, "Loading structures...");
    }

    [RelayCommand]
    public void NewStructure()
    {
        SelectedStructure = null;
        EditName = string.Empty;
        EditDescription = string.Empty;
        EditFields = [];
        HasDefaultParent = false;
        DefaultParentStructure = null;
        DefaultParentRecord = null;
        DefaultParentRecords = [];
        EditPromptField = null;
        EditCompletionField = null;
        IsEditing = true;
    }

    [RelayCommand]
    public async Task EditStructureAsync(DataStructure structure)
    {
        SelectedStructure = structure;
        EditName = structure.Name;
        EditDescription = structure.Description ?? string.Empty;
        EditFields = new ObservableCollection<FieldDefinition>(
            structure.Fields.Select(f => new FieldDefinition
            {
                Name = f.Name, Type = f.Type, Required = f.Required,
                DefaultValue = f.DefaultValue, Description = f.Description
            }));

        // Load default parent rule
        HasDefaultParent = structure.DefaultParentRecordId is not null;
        DefaultParentStructure = null;
        DefaultParentRecord = null;
        DefaultParentRecords = [];

        if (structure.ParentStructureId is not null && structure.DefaultParentRecordId is not null)
        {
            var parentStructure = Structures.FirstOrDefault(s => s.Id == structure.ParentStructureId);
            if (parentStructure is not null)
            {
                DefaultParentStructure = parentStructure;
                var records = await _dataService.GetRecordsAsync(parentStructure.Id);
                DefaultParentRecords = new ObservableCollection<DataRecord>(records);
                DefaultParentRecord = records.FirstOrDefault(r => r.Id == structure.DefaultParentRecordId);
                UpdateDefaultParentLabel();
            }
        }

        EditPromptField = structure.PromptFieldName is not null
            ? EditFields.FirstOrDefault(f => f.Name == structure.PromptFieldName)
            : null;
        EditCompletionField = structure.CompletionFieldName is not null
            ? EditFields.FirstOrDefault(f => f.Name == structure.CompletionFieldName)
            : null;

        IsEditing = true;
    }

    partial void OnDefaultParentStructureChanged(DataStructure? value)
    {
        DefaultParentRecord = null;
        DefaultParentRecords = [];
        DefaultParentLabel = string.Empty;
        if (value is not null)
            _ = LoadDefaultParentRecordsAsync(value);
    }

    partial void OnDefaultParentRecordChanged(DataRecord? value)
        => UpdateDefaultParentLabel();

    private async Task LoadDefaultParentRecordsAsync(DataStructure structure)
    {
        var records = await _dataService.GetRecordsAsync(structure.Id);
        DefaultParentRecords = new ObservableCollection<DataRecord>(records);
    }

    private void UpdateDefaultParentLabel()
    {
        if (DefaultParentRecord is null) { DefaultParentLabel = string.Empty; return; }
        var first = DefaultParentRecord.Fields.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.Value));
        DefaultParentLabel = first.Value ?? $"#{DefaultParentRecord.Id[..6]}";
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            StatusMessage = "Structure name is required.";
            return;
        }

        await RunAsync(async () =>
        {
            var structure = SelectedStructure ?? new DataStructure();
            structure.Name = EditName.Trim();
            structure.Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim();
            structure.Fields = [.. EditFields];

            if (HasDefaultParent && DefaultParentRecord is not null && DefaultParentStructure is not null)
            {
                structure.ParentStructureId = DefaultParentStructure.Id;
                structure.DefaultParentRecordId = DefaultParentRecord.Id;
            }
            else
            {
                structure.ParentStructureId = null;
                structure.DefaultParentRecordId = null;
            }

            structure.PromptFieldName = EditPromptField?.Name;
            structure.CompletionFieldName = EditCompletionField?.Name;

            await _dataService.SaveStructureAsync(structure);
            IsEditing = false;
            await LoadAsync();
        }, "Saving...");
    }

    [RelayCommand]
    public void Cancel()
    {
        IsEditing = false;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    public async Task DeleteAsync(DataStructure structure)
    {
        await RunAsync(async () =>
        {
            await _dataService.DeleteStructureAsync(structure.Id);
            await LoadAsync();
        }, "Deleting...");
    }

    [RelayCommand]
    public async Task ViewRecordsAsync(DataStructure structure)
    {
        await Shell.Current.GoToAsync(nameof(Views.DataRecordsPage),
            new ShellNavigationQueryParameters { { "Structure", structure } });
    }

    [RelayCommand]
    public void AddField()
        => EditFields.Add(new FieldDefinition { Name = $"Field{EditFields.Count + 1}" });

    [RelayCommand]
    public void RemoveField(FieldDefinition field)
        => EditFields.Remove(field);

    public IEnumerable<FieldType> FieldTypes => Enum.GetValues<FieldType>();
}
