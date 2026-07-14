using Godot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Root.Scripts.Globals;
using Root.Scripts.StdOut.Impl;

namespace Root.Scripts.StdOut;

public partial class StdOut : Node
{
	// ReSharper disable once NotAccessedField.Local
	private ILoggerFactory _loggerFactory = null!;

	public override void _Ready()
	{
		Console.SetOut(new StdOutWriter());

#if DEBUG
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
