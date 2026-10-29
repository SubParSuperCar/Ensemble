using System.Globalization;
using EnsembleRoot.Scripts.Adornments;
using EnsembleRoot.Scripts.Assets;
using Godot;
using Serilog;

namespace EnsembleRoot.Tooling.Tools;

/// <summary>
///     Highlights the local plot's hovered instance, and the selected ones with marching ants, while attached to a
///     selection.
/// </summary>
public partial class SelectionHighlights : Node
{
	private readonly SolidHighlight _hoverHighlight = new() { Name = "Hover Highlight", Visible = false };
	private readonly Dictionary<int, SolidHighlight> _selectionHighlights = [];

	private AssetHandle? _hovered;
	private MultiSelector? _selector;

	public Color Tint { get; init; } = Colors.White;

	public int? HoveredId => _hovered is { } hovered && IsLive(hovered) ? hovered.InstanceId : null;

	public override void _Ready()
	{
		_hoverHighlight.Tint = Tint;
		AddChild(_hoverHighlight);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_selector is not null)
			UpdateHover();
	}

	public void Attach(MultiSelector selector)
	{
		_selector = selector;

		OnSelectionAdded([.. selector.InstanceIds]);
		selector.Added += OnSelectionAdded;
		selector.Removed += OnSelectionRemoved;
	}

	public void Detach()
	{
		if (_selector is not { } selector)
			return;

		selector.Added -= OnSelectionAdded;
		selector.Removed -= OnSelectionRemoved;
		OnSelectionRemoved([.. _selectionHighlights.Keys]);

		_selector = null;
		ClearHover();
	}

	public void ClearHover() => SetHovered(null);

	private static bool IsLive(AssetHandle handle) => IsInstanceValid(handle) && !handle.IsQueuedForDeletion();

	private static void Cover(SolidHighlight highlight, AssetHandle handle)
	{
		var aabb = GAssetManager.GetBoundary(handle.AssetId);
		var transform = handle.GlobalTransform;
		transform.Origin = transform * aabb.GetCenter();

		highlight.Aabb = aabb;
		highlight.GlobalTransform = transform;
	}

	private void UpdateHover()
	{
		if (
			ToolCommon.CastCursorRay(GetViewport()) is { Collider: var collider } &&
			ToolCommon.FindInHierarchy<AssetHandle>(collider) is { } handle &&
			ToolCommon.IsHandleLocal(handle))
			SetHovered(handle);
		else
			SetHovered(null);
	}

	private void SetHovered(AssetHandle? handle)
	{
		if (ReferenceEquals(_hovered, handle))
			return;

		_hovered = handle;
		Log.Verbose("Hovered: {Handle}", handle?.Name);

		UpdateHoverHighlight();
	}

	// Selected instances keep their own highlights
	private void UpdateHoverHighlight()
	{
		if (_hovered is not { } handle || !IsLive(handle) || _selector?.Contains(handle.InstanceId) is true)
		{
			_hoverHighlight.Visible = false;
			return;
		}

		Cover(_hoverHighlight, handle);
		_hoverHighlight.Visible = true;
	}

	private void OnSelectionAdded(IReadOnlyList<int> instanceIds)
	{
		foreach (var id in instanceIds)
		{
			if (GPlotManager.LocalHandle?.GetInstanceHandle(id) is not { } handle)
				continue;

			var name = string.Create(CultureInfo.InvariantCulture, $"Selection Highlight {id}");
			var highlight = new SolidHighlight { Name = name, Tint = Tint, MarchingAnts = true };

			AddChild(highlight);

			Cover(highlight, handle);
			_selectionHighlights.Add(id, highlight);
		}

		UpdateHoverHighlight();
	}

	private void OnSelectionRemoved(IReadOnlyList<int> instanceIds)
	{
		foreach (var id in instanceIds)
			if (_selectionHighlights.Remove(id, out var highlight))
				highlight.QueueFree();

		UpdateHoverHighlight();
	}
}
