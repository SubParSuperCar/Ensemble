using System.Runtime.InteropServices;
using EnsembleRoot.Common.Input;
using Godot;

namespace EnsembleRoot.Scripts.Creations;

[StructLayout(LayoutKind.Auto)]
public readonly record struct SimulationTick(InputState Input, InputState PreviousInput, double Delta, bool IsResuming)
{
	public bool IsJustPressed(Key key) => Input.IsPressed(key) && !PreviousInput.IsPressed(key);
}
