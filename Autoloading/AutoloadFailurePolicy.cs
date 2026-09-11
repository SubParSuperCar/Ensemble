namespace Root.Autoloading;

public enum AutoloadFailurePolicy : byte
{
	/// <summary>
	///     Log the event and attempt to ask the user (using a shell dialog) if they want to continue with loading.
	///     Best used for noncritical components that may degrade the user's experience.
	/// </summary>
	AskUser,

	/// <summary>
	///     Log the event and continue with loading.
	///     Best used for noncritical components that have negligible impacts on the user's experience.
	/// </summary>
	LogAndContinue,

	/// <summary>
	///     Log the event and immediately terminate the application.
	///     Best used for critical components that the application cannot function without.
	/// </summary>
	FailFast
}
