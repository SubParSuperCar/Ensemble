namespace EnsembleRoot.Autoloading;

/// <summary>
///     What to do if this Autoload fails to be instantiated, added to the tree, or initialized.
/// </summary>
public enum AutoloadFailurePolicy : byte
{
	/// <summary>
	///     Log the failure and ask the user, through a native dialog, whether to continue loading; terminate if they
	///     decline or the dialog fails. Best for noncritical components whose loss degrades the experience.
	/// </summary>
	AskUser,

	/// <summary>
	///     Log the failure and continue loading. Best for components whose loss is negligible.
	/// </summary>
	LogAndContinue,

	/// <summary>
	///     Log the failure and terminate immediately. Best for critical components the application can't run without.
	/// </summary>
	FailFast
}
