using CommunityToolkit.Mvvm.ComponentModel;
using SecureBird_Data_Store.Models;

namespace SecureBird_Data_Store.ViewModels;

public partial class HierarchyNodeViewModel : ObservableObject
{
    public DataRecord Record { get; init; } = null!;
    public string StructureName { get; init; } = string.Empty;
    public string StructureId { get; init; } = string.Empty;
    public string DisplayLabel { get; init; } = string.Empty;
    public int Depth { get; init; }
    public bool HasChildren { get; init; }

    [ObservableProperty]
    private bool _isExpanded;

    public Thickness IndentMargin => new(Depth * 24 + 12, 0, 12, 0);
    public string ExpandIcon => HasChildren ? (IsExpanded ? "▼" : "▶") : "  ";
    public string StructureColor => DepthColors[Depth % DepthColors.Length];

    private static readonly string[] DepthColors =
        ["#512BD4", "#2B0B98", "#217346", "#C25E00", "#8B008B"];

    public static string BuildLabel(DataRecord record, DataStructure? structure)
    {
        if (structure?.Fields.Count > 0)
        {
            var key = structure.Fields[0].Name;
            if (record.Fields.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                return val;
        }
        var first = record.Fields.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.Value));
        return first.Value ?? $"#{record.Id[..6]}";
    }
}
