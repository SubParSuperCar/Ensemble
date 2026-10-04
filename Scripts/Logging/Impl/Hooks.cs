using Serilog.Sinks.File.Header;

namespace EnsembleRoot.Scripts.Logging.Impl;

public static class Hooks
{
	public static HeaderWriter Header =>
		new("{\"@header\":\"This is an Ensemble Serilog file: " + GitHubRepoUrl + "\"}");
}
