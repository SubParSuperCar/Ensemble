namespace Root.SessionManager.Actions;

public readonly record struct ActionValidation(bool IsValid, string? Reason = null)
{
	public static readonly ActionValidation Accept = new(true);
	public static ActionValidation Reject(string reason) => new(false, reason);
}
