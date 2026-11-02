using System.Runtime.InteropServices;
using EnsembleRoot.Common.Input;

namespace EnsembleRoot.Scripts.Creations;

/// <summary>What an asset's logic works from during one physics tick of its creation.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct SimulationTick(InputState Input, double Delta);
