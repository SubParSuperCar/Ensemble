using EnsembleRoot.SessionManager.Api;

namespace EnsembleRoot.SessionManager.Auth;

public static class Authenticators
{
	public static IPeerAuthenticator? Password(string? password) =>
		string.IsNullOrEmpty(password) ? null : new PasswordAuthenticator(password);
}
