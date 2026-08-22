namespace Root.Autoloading;

public enum AutoloadFailurePolicy : byte
{
	LogAndContinue, // Log it and continue. Good for non-critical/unimportant components.
	FailFast, // Immediately terminate the process because a critical component failed. The program cannot run.
	AskUser // The failing component is quite important but is ultimately up to the user.
}
