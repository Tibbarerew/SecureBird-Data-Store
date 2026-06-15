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
        IsEditing = true;
    }

    [RelayCommand]
    public void EditStructure(DataStructure structure)
    {
        SelectedStructure = structure;
        EditName = structure.Name;
        EditDescription = structure.Description ?? string.Empty;
        EditFields = new ObservableCollection<FieldDefinition>(
            structure.Fields.Select(f => new FieldDefinition
            {
                Name = f.Name,
                Type = f.Type,
                Required = f.Required,
                DefaultValue = f.DefaultValue,
                Description = f.Description
            }));
        IsEditing = true;
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
    public void AddField()
    {
        EditFields.Add(new FieldDefinition { Name = $"Field{EditFields.Count + 1}" });
    }

    [RelayCommand]
    public void RemoveField(FieldDefinition field)
    {
        EditFields.Remove(field);
    }

    public IEnumerable<FieldType> FieldTypes => Enum.GetValues<FieldType>();
}
