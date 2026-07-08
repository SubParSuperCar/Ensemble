using System.Globalization;
using System.Text;
using Godot;

namespace Root.Scripts.StdOut.Impl;

public class StdOutWriter : TextWriter
{
	public override Encoding Encoding => Encoding.UTF8;

	public override void Write(string? value) => Print(value);
	public override void WriteLine(string? value) => Print(value);

	private static void Print(string? message) =>
		GD.Print(string.Create(CultureInfo.InvariantCulture, $"[{DateTime.Now:HH:mm:ss.fff}]: {message}"));
}
