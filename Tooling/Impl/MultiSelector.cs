using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.GdCore.Plots;

namespace EnsembleRoot.Tooling;

/// <summary>
///     The local plot's selected instances, shared by every tool so a selection carries across them. Removed instances
///     leave it, and it clears when the local plot changes.
/// </summary>
public sealed class MultiSelector : IDisposable
{
	private readonly HashSet<int> _instanceIds = [];

	private GdPlot? _plot;

	public IReadOnlyCollection<int> InstanceIds => _instanceIds;

	public void Dispose()
	{
		LocalPlotChanged -= OnLocalPlotChanged;
		OnLocalPlotChanged(null);
	}

	public event Action<IReadOnlyList<int>>? Added;
	public event Action<IReadOnlyList<int>>? Removed;

	public void Initialize()
	{
		OnLocalPlotChanged(LocalPlot);
		LocalPlotChanged += OnLocalPlotChanged;
	}

	public bool Contains(int instanceId) => _instanceIds.Contains(instanceId);

	// ReSharper disable once UnusedMember.Global
	public void Add(IEnumerable<int> instanceIds) => Select([.. instanceIds.Distinct().Where(IsSelectable)]);

	// ReSharper disable once MemberCanBePrivate.Global
	public void Remove(IEnumerable<int> instanceIds) => Deselect([.. instanceIds.Distinct().Where(Contains)]);
	public void Clear() => Deselect([.. _instanceIds]);

	public void Set(IEnumerable<int> instanceIds)
	{
		var set = instanceIds.ToHashSet();

		Deselect([.. _instanceIds.Where(id => !set.Contains(id))]);
		Select([.. set.Where(IsSelectable)]);
	}

	public void Toggle(IEnumerable<int> instanceIds)
	{
		var distinct = instanceIds.Distinct().ToArray();
		int[] removed = [.. distinct.Where(Contains)];
		int[] added = [.. distinct.Where(IsSelectable)];

		Deselect(removed);
		Select(added);
	}

	/// <summary>
	///     Applies a trigger press and returns the instances it acts on (Baja Builders parity). Toggling only edits the
	///     selection. With a selection, pressing a selected instance acts on all of them and anything else clears it;
	///     without one, the press acts on the hovered instance.
	/// </summary>
	public IReadOnlyList<int> Trigger(int? hoveredId, bool isToggling)
	{
		if (isToggling)
		{
			if (hoveredId is { } id)
				Toggle([id]);

			return [];
		}

		if (_instanceIds.Count is 0)
			return hoveredId is { } hovered ? [hovered] : [];

		if (hoveredId is { } selected && Contains(selected))
			return [.. _instanceIds];

		Clear();
		return [];
	}

	private bool IsSelectable(int instanceId) =>
		!Contains(instanceId) && _plot?.Instances.Source.TryGet(instanceId, out _) is true;

	private void Select(int[] instanceIds)
	{
		if (instanceIds.Length is 0)
			return;

		_instanceIds.UnionWith(instanceIds);
		Added?.Invoke(instanceIds);
	}

	private void Deselect(int[] instanceIds)
	{
		if (instanceIds.Length is 0)
			return;

		_instanceIds.ExceptWith(instanceIds);
		Removed?.Invoke(instanceIds);
	}

	private void OnLocalPlotChanged(GdPlot? plot)
	{
		Clear();

		_plot?.Instances.Removed -= OnInstanceRemoved;
		_plot = plot;
		plot?.Instances.Removed += OnInstanceRemoved;
	}

	private void OnInstanceRemoved(GdInstance instance) => Remove([instance.Id]);
}
