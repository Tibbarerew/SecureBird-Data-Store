using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureBird_Data_Store.Models;
using SecureBird_Data_Store.Services;

namespace SecureBird_Data_Store.ViewModels;

public partial class HierarchyViewModel : BaseViewModel
{
    private readonly IJsonDataService _dataService;

    // Full tree keyed by parentRecordId (null key = roots)
    private Dictionary<string?, List<HierarchyNodeViewModel>> _tree = [];
    private Dictionary<string, DataStructure> _structureMap = [];

    [ObservableProperty]
    private ObservableCollection<HierarchyNodeViewModel> _visibleNodes = [];

    [ObservableProperty]
    private int _totalRecords;

    [ObservableProperty]
    private int _totalStructures;

    public HierarchyViewModel(IJsonDataService dataService)
    {
        _dataService = dataService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            var structures = await _dataService.GetStructuresAsync();
            var allRecords = await _dataService.GetAllRecordsAsync();

            _structureMap = structures.ToDictionary(s => s.Id);
            TotalStructures = structures.Count;
            TotalRecords = allRecords.Count;

            // Build child-lookup: parentRecordId → [child nodes]
            _tree = new Dictionary<string?, List<HierarchyNodeViewModel>>();
            var childIds = new HashSet<string>(
                allRecords.Where(r => r.ParentRecordId != null).Select(r => r.ParentRecordId!));

            foreach (var record in allRecords)
            {
                _structureMap.TryGetValue(record.StructureId, out var structure);
                var node = new HierarchyNodeViewModel
                {
                    Record = record,
                    StructureId = record.StructureId,
                    StructureName = structure?.Name ?? "Unknown",
                    DisplayLabel = HierarchyNodeViewModel.BuildLabel(record, structure),
                    Depth = 0, // recalculated during flattening
                    HasChildren = childIds.Contains(record.Id)
                };

                var parentKey = record.ParentRecordId;
                if (!_tree.ContainsKey(parentKey))
                    _tree[parentKey] = [];
                _tree[parentKey].Add(node);
            }

            // Build visible list starting from roots (ParentRecordId == null)
            var visible = new List<HierarchyNodeViewModel>();
            AddNodesAtDepth(visible, null, 0);
            VisibleNodes = new ObservableCollection<HierarchyNodeViewModel>(visible);
        }, "Loading hierarchy...");
    }

    private void AddNodesAtDepth(
        List<HierarchyNodeViewModel> target,
        string? parentKey,
        int depth)
    {
        if (!_tree.TryGetValue(parentKey, out var children)) return;

        foreach (var child in children.OrderBy(n => n.DisplayLabel))
        {
            // Rebuild with correct depth
            var node = new HierarchyNodeViewModel
            {
                Record = child.Record,
                StructureId = child.StructureId,
                StructureName = child.StructureName,
                DisplayLabel = child.DisplayLabel,
                Depth = depth,
                HasChildren = child.HasChildren
            };
            target.Add(node);
        }
    }

    [RelayCommand]
    public void ToggleNode(HierarchyNodeViewModel node)
    {
        if (!node.HasChildren) return;

        var index = VisibleNodes.IndexOf(node);
        if (index < 0) return;

        if (node.IsExpanded)
        {
            CollapseNode(node, index);
        }
        else
        {
            ExpandNode(node, index);
        }
    }

    private void ExpandNode(HierarchyNodeViewModel node, int index)
    {
        var toInsert = new List<HierarchyNodeViewModel>();
        AddNodesAtDepth(toInsert, node.Record.Id, node.Depth + 1);

        for (int i = 0; i < toInsert.Count; i++)
            VisibleNodes.Insert(index + 1 + i, toInsert[i]);

        node.IsExpanded = true;
    }

    private void CollapseNode(HierarchyNodeViewModel node, int index)
    {
        // Remove all subsequent nodes that are deeper than this node
        int removeFrom = index + 1;
        while (removeFrom < VisibleNodes.Count &&
               VisibleNodes[removeFrom].Depth > node.Depth)
        {
            VisibleNodes.RemoveAt(removeFrom);
        }
        node.IsExpanded = false;
    }
}
