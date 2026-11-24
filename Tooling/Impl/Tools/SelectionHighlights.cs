using System.Globalization;
using EnsembleRoot.Scripts.Adornments;
using EnsembleRoot.Scripts.Assets;
using Godot;
using Serilog;

namespace EnsembleRoot.Tooling.Tools;

/// <summary>Highlights the local plot's hovered instance and the selected ones while attached to a selection.</summary>
public partial class SelectionHighlights : Node
{
	private readonly SolidHighlight _hoverHighlight = new() { Name = "Hover Highlight", Visible = false };

	private readonly Dictionary<int, SolidHighlight> _selectionHighlights = [];

	private Func<int, bool>? _canHover;
	private AssetHandle? _hovered;
	private SolidHighlight? _pulsingSelection;
	private MultiSelector? _selector;

	public Color Tint
	{
		get;
		set
		{
			field = value;
			_hoverHighlight.Tint = value;

			foreach (var (_, highlight) in _selectionHighlights)
				highlight.Tint = value;
		}
	} = Colors.White;

	public int? HoveredId => _hovered is { } hovered && IsLive(hovered) ? hovered.InstanceId : null;

	public override void _Ready()
	{
		_hoverHighlight.Tint = Tint;
		_hoverHighlight.Pulsing = true;
		AddChild(_hoverHighlight);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_selector is not null)
			UpdateHover();
	}

	/// <summary>Highlights a selection, and hovered instances of assets <paramref name="canHover" /> takes.</summary>
	public void Attach(MultiSelector selector, Func<int, bool> canHover)
	{
		_selector = selector;
		_canHover = canHover;

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
		_canHover = null;
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
		highlight.Target = handle;
	}

	private void UpdateHover()
	{
		if (
			ToolCommon.CastCursorRay(GetViewport()) is { Collider: var collider } &&
			ToolCommon.FindInHierarchy<AssetHandle>(collider) is { } handle &&
			ToolCommon.IsHandleLocal(handle) && _canHover?.Invoke(handle.AssetId) is true)
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

	// A selected instance pulses its own highlight instead
	private void UpdateHoverHighlight()
	{
		var handle = _hovered is { } hovered && IsLive(hovered) ? hovered : null;
		var selection = handle is null ? null : _selectionHighlights.GetValueOrDefault(handle.InstanceId);

		if (!ReferenceEquals(_pulsingSelection, selection))
		{
			_pulsingSelection?.Pulsing = false;
			_pulsingSelection = selection;
			_pulsingSelection?.Pulsing = true;
		}

		if (handle is null || selection is not null)
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
			if (ToolCommon.LocalHandle?.GetInstanceHandle(id) is not { } handle)
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
