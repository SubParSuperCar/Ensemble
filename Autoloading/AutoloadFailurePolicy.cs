namespace EnsembleRoot.Autoloading;

/// <summary>
///     The policy to apply if this Autoload fails and cannot be instantiated.
/// </summary>
public enum AutoloadFailurePolicy : byte
{
	/// <summary>
	///     Log the event and attempt to ask the user, through a shell dialog, whether to continue loading.
	///     Best used for noncritical components that may degrade the user's experience.
	/// </summary>
	AskUser,

	/// <summary>
	///     Log the event and continue with loading.
	///     Best used for noncritical components that have a negligible impact on the user's experience.
	/// </summary>
	LogAndContinue,

	/// <summary>
	///     Log the event and immediately terminate the application.
	///     Best used for critical components that the application cannot function without.
	/// </summary>
	FailFast
}
