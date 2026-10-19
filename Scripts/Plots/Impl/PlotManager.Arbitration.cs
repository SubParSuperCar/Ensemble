using System.Runtime.InteropServices;
using EnsembleRoot.Scripts.Plots.Impl;
using Serilog;

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotManager
{
	// Plots stay linked this long after their creations part, and arbitration moves a plot at most this often, so
	// creations jostling at the edge don't flap between simulators
	private const double ArbitrationHoldTime = 2d;

	private const double ArbitrationReviewInterval = 1 / 8d;

	private readonly Dictionary<int, double> _arbitrationTimesByPlotId = [];
	private readonly Dictionary<(int, int), PlotLink> _links = [];
	private readonly List<PlotHandle> _spawnedHandles = [];

	private double _arbitrationClock;
	private double _sinceArbitrationReview;

	/// <summary>Ends a despawning plot's links at once, as its creation is gone.</summary>
	internal void ForgetArbitration(PlotHandle plot)
	{
		foreach (var key in _links.Keys.Where(key => key.Item1 == plot.Id || key.Item2 == plot.Id).ToList())
			_links.Remove(key);

		_arbitrationTimesByPlotId.Remove(plot.Id);
	}

	private void UpdateArbitration(double delta)
	{
		if (!PlotHandle.IsArbitrating)
			return;

		_arbitrationClock += delta;
		_sinceArbitrationReview += delta;

		if (_sinceArbitrationReview < ArbitrationReviewInterval)
			return;

		_sinceArbitrationReview = 0;

		_spawnedHandles.Clear();
		_spawnedHandles.AddRange(_handlesById.Values.Where(static handle => handle.Assemblies.Count is not 0));

		LinkCloseCreations();
		ReviewArbitration();
	}

	// Only while two or more creations are spawned is there anything to compare
	private void LinkCloseCreations()
	{
		if (_spawnedHandles.Count < 2)
			return;

		foreach (var handle in _spawnedHandles)
			handle.UpdateArbitrationSpheres();

		for (var i = 0; i < _spawnedHandles.Count; i++)
			for (var j = i + 1; j < _spawnedHandles.Count; j++)
			{
				var (plot, other) = (_spawnedHandles[i], _spawnedHandles[j]);
				var key = (Math.Min(plot.Id, other.Id), Math.Max(plot.Id, other.Id));
				var isOverlapping = plot.ArbitrationSpheres.Overlaps(other.ArbitrationSpheres);

				// Parted links keep the time they parted
				if (_links.TryGetValue(key, out var link) ? link.IsOverlapping != isOverlapping : isOverlapping)
					_links[key] = new PlotLink(isOverlapping, _arbitrationClock);
			}
	}

	// Each group of linked plots goes to the simulator its creation with the most assemblies has apart from
	// arbitration, which already carries the heaviest load, so the work stays spread across players
	private void ReviewArbitration()
	{
		foreach (var (key, link) in _links)
			if (!link.IsOverlapping && _arbitrationClock - link.ChangeTime >= ArbitrationHoldTime)
				_links.Remove(key);

		var plots = _spawnedHandles.Select(static handle => (handle.Id, handle.Assemblies.Count)).ToList();

		foreach (var (id, leaderId) in Arbitration.GetLeaders(plots, _links.Keys))
		{
			var handle = _handlesById[id];
			var leaderSimulator = _handlesById[leaderId].BaseSimulatorPeerId;
			int? simulator = leaderSimulator == handle.BaseSimulatorPeerId ? null : leaderSimulator;

			if (simulator == handle.ArbitratedSimulatorPeerId || IsArbitrationHeld(id))
				continue;

			_arbitrationTimesByPlotId[id] = _arbitrationClock;
			handle.SetArbitratedSimulator(simulator);

			if (simulator is null)
				Log.Debug("Plot {PlotId}'s creation left arbitration", id);
			else
				Log.Debug(
					"Plot {PlotId}'s creation is close to plot {LeaderId}'s, so peer {PeerId} simulates both",
					id, leaderId, simulator);
		}
	}

	private bool IsArbitrationHeld(int plotId) =>
		_arbitrationTimesByPlotId.TryGetValue(plotId, out var time) && _arbitrationClock - time < ArbitrationHoldTime;

	// Once the creations no longer overlap, the change time is when they parted
	[StructLayout(LayoutKind.Auto)]
	private readonly record struct PlotLink(bool IsOverlapping, double ChangeTime);
}
