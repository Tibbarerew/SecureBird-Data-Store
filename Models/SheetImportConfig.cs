using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SecureBird_Data_Store.Models;

public partial class SheetImportConfig : ObservableObject
{
    public ExcelSheetPreview Sheet { get; init; } = null!;
    public string SheetName => Sheet.SheetName;
    public string SheetSummary => $"{Sheet.TotalRows} rows · {Sheet.Headers.Count} columns";

    [ObservableProperty]
    private bool _include = true;

    [ObservableProperty]
    private bool _createNew = true;

    [ObservableProperty]
    private string _structureName = string.Empty;

    [ObservableProperty]
    private DataStructure? _existingStructure;

    // Parent linking
    [ObservableProperty]
    private bool _hasParent;

    [ObservableProperty]
    private DataStructure? _parentStructure;

    [ObservableProperty]
    private ObservableCollection<DataRecord> _availableParentRecords = [];

    [ObservableProperty]
    private DataRecord? _selectedParentRecord;

    [ObservableProperty]
    private string _parentRecordLabel = "None (top-level)";

    public void SetAvailableParentRecords(List<DataRecord> records, DataStructure? structure)
    {
        AvailableParentRecords = new ObservableCollection<DataRecord>(records);
        SelectedParentRecord = null;
        ParentRecordLabel = "Select a record...";
    }

    partial void OnSelectedParentRecordChanged(DataRecord? value)
    {
        if (value is null)
        {
            ParentRecordLabel = "None (top-level)";
            return;
        }
        var first = value.Fields.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.Value));
        ParentRecordLabel = first.Value ?? $"#{value.Id[..6]}";
    }

    partial void OnHasParentChanged(bool value)
    {
        if (!value)
        {
            ParentStructure = null;
            SelectedParentRecord = null;
            ParentRecordLabel = "None (top-level)";
        }
    }
}
