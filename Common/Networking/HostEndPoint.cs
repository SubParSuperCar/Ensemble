using System.Globalization;

namespace EnsembleRoot.Common.Networking;

/// <summary>
///     A host and port parsed from text such as <c>192.168.1.2:7777</c>, <c>[::1]:7777</c>, or <c>example.com:7777</c>.
/// </summary>
public readonly record struct HostEndPoint(string Host, int Port)
{
	public static bool TryParse(string? value, out HostEndPoint endPoint)
	{
		endPoint = default;

		if (
			string.IsNullOrWhiteSpace(value) ||
			!Uri.TryCreate($"udp://{value.Trim()}", UriKind.Absolute, out var uri) ||
			uri is not { Port: > 0, PathAndQuery: "/", UserInfo: "", Fragment: "" })
			return false;

		endPoint = new HostEndPoint(uri.IdnHost, uri.Port);
		return true;
	}

	public override string ToString() =>
		string.Create(
			CultureInfo.InvariantCulture,
			$"{(Host.Contains(':', StringComparison.Ordinal) ? $"[{Host}]" : Host)}:{Port}");
}
