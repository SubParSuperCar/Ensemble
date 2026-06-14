using System.Text;
using Godot;

namespace Root.Gd.StdOut.Impl;

public class StdOutWriter : TextWriter
{
	public override Encoding Encoding => Encoding.UTF8;

	public override void WriteLine(string? value) => GD.Print(value);
	public override void Write(string? value) => GD.Print(value);
}
