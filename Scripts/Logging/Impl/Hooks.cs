using Serilog.Sinks.File.Header;

namespace Root.Scripts.Logging.Impl;

// ReSharper disable once UnusedType.Global
public static class Hooks
{
	// ReSharper disable once UnusedMember.Global
	public static HeaderWriter Header =>
		new("{\"@header\":\"This is an Ensemble Serilog file: https://github.com/SubParSuperCar/Ensemble\"}");
}
