using EnsembleRoot.Common.Networking;
using EnsembleRoot.Scripts.Assets;

namespace EnsembleRoot.Scripts.Creations;

public partial class LiveCreation
{
	// Twice what an honest simulator sends, at most one batch per stream
	private const double AssetStateRelaysPerSecond = 2d * StreamRate;

	private const int UnsentAssetState = int.MinValue;

	private readonly TokenGate _assetStateGate = new(AssetStateRelaysPerSecond, GTimeProvider);
	private readonly List<int> _changedAssetStates = [];
	private readonly List<IReplicatedState> _replicatedStates = [];
	private readonly List<int> _sentAssetStates = [];

	/// <summary>Every asset's state, as pairs of its index in build order and its value.</summary>
	internal int[] CaptureAssetStates()
	{
		var states = new int[2 * _replicatedStates.Count];

		for (var i = 0; i < _replicatedStates.Count; i++)
		{
			states[2 * i] = i;
			states[2 * i + 1] = _replicatedStates[i].State;
		}

		return states;
	}

	internal bool TryReceiveAssetStates(int[] states, bool isRelaying)
	{
		if (
			states.Length > 2 * _replicatedStates.Count || states.Length % 2 is not 0 ||
			(isRelaying && !_assetStateGate.TryTake()))
			return false;

		for (var i = 0; i < states.Length; i += 2)
			if ((uint)states[i] < (uint)_replicatedStates.Count)
				_replicatedStates[states[i]].State = states[i + 1];

		return true;
	}

	private void AddReplicatedStates(AssetHandle handle)
	{
		if (handle is IReplicatedState own)
			AddReplicatedState(own);

		// ReSharper disable once SuspiciousTypeConversion.Global
		foreach (var component in handle.Components.OfType<IReplicatedState>())
			AddReplicatedState(component);
	}

	private void AddReplicatedState(IReplicatedState state)
	{
		_replicatedStates.Add(state);
		_sentAssetStates.Add(state.State);
	}

	private void SendAssetStates()
	{
		_changedAssetStates.Clear();

		for (var i = 0; i < _replicatedStates.Count; i++)
		{
			var state = _replicatedStates[i].State;

			if (state == _sentAssetStates[i])
				continue;

			_sentAssetStates[i] = state;
			_changedAssetStates.Add(i);
			_changedAssetStates.Add(state);
		}

		if (_changedAssetStates.Count > 0)
			Plot.SendAssetStates([.. _changedAssetStates]);
	}

	// A new simulator sends them all, which also corrects what the host refused from the old one mid-hand-off
	private void ForgetSentAssetStates()
	{
		for (var i = 0; i < _sentAssetStates.Count; i++)
			_sentAssetStates[i] = UnsentAssetState;
	}
}
