using System.Text;
using Godot;

namespace Root.Gd.StdOut.Impl;

public class StdOutWriter : TextWriter
{
	public override Encoding Encoding => Encoding.UTF8;

	public override void Write(string? value) => Print(value);
	public override void WriteLine(string? value) => Print(value);

	private static void Print(string? what) => GD.Print($"[{DateTime.Now:HH:mm:ss.ffff}]: {what}");
}
