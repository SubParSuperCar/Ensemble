using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace EnsembleRoot.Sessions;

/// <summary>
///     Identifies a remote host for bans and password throttling: by its IPv4 address, or by its IPv6 /64 prefix
///     (e.g., <c>2001:db8::/64</c>), since IPv6 hosts pick addresses within their /64 freely.
/// </summary>
public static class AddressKey
{
	private const string SubnetSuffix = "/64";

	// Stricter than IPAddress.TryParse, which takes shorthands like "12345" (0.0.48.57), and takes back an IPv6 key
	public static bool TryCreate(string address, [NotNullWhen(true)] out string? key)
	{
		key = null;

		var hasSuffix = address.EndsWith(SubnetSuffix, StringComparison.Ordinal);
		var text = hasSuffix ? address[..^SubnetSuffix.Length] : address;

		if (!IPAddress.TryParse(text, out var ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any))
			return false;

		if (ip.AddressFamily is AddressFamily.InterNetwork || ip.IsIPv4MappedToIPv6)
		{
			key = !hasSuffix && (ip.IsIPv4MappedToIPv6 || text.AsSpan().Count('.') is 3)
				? ip.MapToIPv4().ToString()
				: null;

			return key is not null;
		}

		var bytes = ip.GetAddressBytes();
		bytes.AsSpan(8).Clear();
		key = string.Create(CultureInfo.InvariantCulture, $"{new IPAddress(bytes)}{SubnetSuffix}");

		return true;
	}
}
