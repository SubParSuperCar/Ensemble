using Root.SessionManager.Api;

namespace Root.SessionManager.Auth;

public static class Authenticators
{
	// Maybe we can add a resource for generating passwords or codes automatically? Not important.
	public static IPeerAuthenticator? Password(string? password) =>
		string.IsNullOrEmpty(password) ? null : new PasswordAuthenticator(password);
}
