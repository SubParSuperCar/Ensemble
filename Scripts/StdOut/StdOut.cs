using Godot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Root.Scripts.Globals;
using Root.Scripts.StdOut.Impl;

namespace Root.Scripts.StdOut;

// Intercepts all C# output, prefixes it with a simple timestamp, and re-routes it to the Godot output
// Rider shows all Godot output in the console, so C# output still shows up in the editor
public partial class StdOut : Node
{
	// ReSharper disable once NotAccessedField.Local
	private ILoggerFactory _loggerFactory = null!;

	public override void _Ready()
	{
		Console.SetOut(new StdOutWriter());

#if DEBUG
		// Sets the logger to the stored level, if it exists
		// Currently it's set to 'Trace' for enhanced debugging
		var configuration = new ConfigurationBuilder()
			.SetBasePath(ProjectSettings.GlobalizePath(ScriptConstants.ResourceScheme))
			.AddJsonFile("appsettings.json", true, true)
			.Build();

		_loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.AddConsole();
			builder.AddDebug();
		});
#endif
	}
}
