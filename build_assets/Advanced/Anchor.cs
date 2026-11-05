using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Creations;
using Godot;

namespace EnsembleRoot.BuildAssets.Advanced;

public partial class Anchor : AssetHandle, ISimulationTicked, IReplicatedState
{
	private const string IsAnchoredKey = "isAnchored";
	private const string ToggleBinding = "toggleKey";

	private bool _isAnchored;
	private Key _toggleKey;

	public int State
	{
		get => _isAnchored ? 1 : 0;
		set => SetAnchored(value is not 0);
	}

	public void Tick(in SimulationTick tick)
	{
		if (tick.IsJustPressed(_toggleKey))
			SetAnchored(!_isAnchored);
	}

	protected override void OnInitialized()
	{
		if (!IsLive)
			return;

		_toggleKey = GetKeyProperty(ToggleBinding);
		SetAnchored(Instance.Properties.GetValue(IsAnchoredKey).AsBool());
	}

	private void SetAnchored(bool isAnchored)
	{
		_isAnchored = isAnchored;
		Assembly!.SetHeld(this, isAnchored);
	}
}
