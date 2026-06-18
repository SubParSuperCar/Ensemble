using Godot;
using Root.Gd.StdOut.Impl;

namespace Root.Gd.StdOut;

public partial class StdOut : Node
{
	public override void _Ready() => Console.SetOut(new StdOutWriter());
}
