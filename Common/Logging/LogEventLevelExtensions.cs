using Serilog.Events;

namespace EnsembleRoot.Common.Logging;

public static class LogEventLevelExtensions
{
	extension(LogEventLevel level)
	{
		public string Abbreviation =>
			level switch
			{
				LogEventLevel.Verbose => "VRB",
				LogEventLevel.Debug => "DBG",
				LogEventLevel.Information => "INF",
				LogEventLevel.Warning => "WRN",
				LogEventLevel.Error => "ERR",
				LogEventLevel.Fatal => "FTL",
				_ => "???"
			};
	}
}
