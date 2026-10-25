using EnsembleRoot.GdCore.Plots;
using Edit = (System.Action Undo, System.Action Redo);

namespace EnsembleRoot.Tooling;

/// <summary>
///     The tools' edit history on the local plot, cleared when the local plot changes. Each edit pairs thunks that
///     submit actions, so undoing and redoing are validated (and may be rejected) like any other edit.
/// </summary>
public sealed class UndoRedo : IDisposable
{
	private const int MaxCount = 256;

	private readonly Stack<Edit> _redos = [];
	private readonly List<Edit> _undos = [];

	public bool CanUndo => _undos.Count > 0 && IsLocalPlotSpawned is false;
	public bool CanRedo => _redos.Count > 0 && IsLocalPlotSpawned is false;

	public void Dispose()
	{
		LocalPlotChanged -= OnLocalPlotChanged;
		IsLocalPlotSpawnedChanged -= OnIsLocalPlotSpawnedChanged;
	}

	public event Action? Changed;

	public void Initialize()
	{
		LocalPlotChanged += OnLocalPlotChanged;
		IsLocalPlotSpawnedChanged += OnIsLocalPlotSpawnedChanged;
	}

	/// <summary>Runs an edit and records it, forgetting the undone edits and the oldest past the limit.</summary>
	public void Do(Action redo, Action undo)
	{
		redo();

		_redos.Clear();
		_undos.Add((undo, redo));

		if (_undos.Count > MaxCount)
			_undos.RemoveAt(0);

		Changed?.Invoke();
	}

	public void Undo()
	{
		if (!CanUndo)
			return;

		var edit = _undos[^1];
		_undos.RemoveAt(_undos.Count - 1);
		_redos.Push(edit);

		edit.Undo();
		Changed?.Invoke();
	}

	public void Redo()
	{
		if (!CanRedo)
			return;

		var edit = _redos.Pop();
		_undos.Add(edit);

		edit.Redo();
		Changed?.Invoke();
	}

	private void Clear()
	{
		if (_undos.Count is 0 && _redos.Count is 0)
			return;

		_undos.Clear();
		_redos.Clear();

		Changed?.Invoke();
	}

	private void OnLocalPlotChanged(GdPlot? _) => Clear();
	private void OnIsLocalPlotSpawnedChanged(bool? _) => Changed?.Invoke();
}
