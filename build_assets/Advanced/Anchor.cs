using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Creations;
using Godot;

namespace EnsembleRoot.BuildAssets.Advanced;

/// <summary>
///     Freezes its assembly in place while anchored, as long as any anchor on it is. Its key switches it, and it starts
///     as the instance's property says.
/// </summary>
public partial class Anchor : AssetHandle, ISimulationTicked
{
	private const string IsAnchoredKey = "isAnchored";
	private const string ToggleBinding = "toggleKey";

	private bool _isAnchored;
	private bool _isKeyHeld;
	private Key _toggleKey;

	public void Tick(in SimulationTick tick)
	{
		var isPressed = tick.Input.IsPressed(_toggleKey);

		if (isPressed && !_isKeyHeld)
			SetAnchored(!_isAnchored);

		_isKeyHeld = isPressed;
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
