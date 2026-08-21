namespace Root.Autoloading;

public enum AutoloadFailurePolicy : byte
{
	LogAndContinue,
	FailFast,
	AskUser
}
