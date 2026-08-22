using Root.SessionManager.Api;

namespace Root.SessionManager.Auth;

public static class Authenticators
{
	public static IPeerAuthenticator? Password(string? password) =>
		string.IsNullOrEmpty(password) ? null : new PasswordAuthenticator(password);
}
