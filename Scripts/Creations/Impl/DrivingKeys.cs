using Godot;

namespace EnsembleRoot.Scripts.Creations;

/// <summary>
///     A pair of keys driving a mechanism one way or the other: while held, or, if they toggle, from a press until the
///     other key reverses it or the same key stops it. Neither held, or both, drives nothing.
/// </summary>
public sealed class DrivingKeys(Key forward, Key reverse, bool toggles)
{
	/// <summary>Which way the keys drive: 1 forward, -1 in reverse, or 0.</summary>
	public int Direction { get; private set; }

	/// <summary>Whether both keys are held against each other, which cancels them out.</summary>
	public bool IsOpposed { get; private set; }

	/// <summary>The latched direction, an asset state: only a toggle's latch outlasts the keys that set it.</summary>
	public int State
	{
		get => toggles ? Direction : 0;
		set
		{
			if (toggles)
				Direction = Math.Sign(value);
		}
	}

	public void Update(in SimulationTick tick)
	{
		var isForward = tick.Input.IsPressed(forward);
		var isReverse = tick.Input.IsPressed(reverse);

		Direction = toggles ? GetToggledDirection(tick) : GetSign(isForward, isReverse);
		IsOpposed = !toggles && isForward && isReverse;
	}

	private static int GetSign(bool isForward, bool isReverse) => (isForward ? 1 : 0) - (isReverse ? 1 : 0);

	// Only new presses count, and pressing both at once changes nothing, as holding both does otherwise
	private int GetToggledDirection(in SimulationTick tick)
	{
		var pressed = GetSign(tick.IsJustPressed(forward), tick.IsJustPressed(reverse));

		return pressed is 0 ? Direction : pressed == Direction ? 0 : pressed;
	}
}
