using EnsembleRoot.Common.Networking;
using EnsembleRoot.Common.Utils;

namespace EnsembleRoot.Scripts.Sessions.Impl;

// Passwords aren't remembered, since the user data is plain text
public static class SessionPreferences
{
	private const string Section = "session";
	private const string DisplayNameKey = "display_name";
	private const string IsUpnpEnabledKey = "upnp_enabled";
	private const string HostPortKey = "host_port";
	private const string JoinEndPointKey = "join_end_point";
	private const string IsJoinByCodeKey = "join_by_code";

	public static string DisplayName
	{
		get => UserData.GetValue(Section, DisplayNameKey, string.Empty).AsString();
		set => UserData.SetValue(Section, DisplayNameKey, value);
	}

	public static bool IsUpnpEnabled
	{
		get => UserData.GetValue(Section, IsUpnpEnabledKey, true).AsBool();
		set => UserData.SetValue(Section, IsUpnpEnabledKey, value);
	}

	public static int? HostPort
	{
		get => UserData.GetValue(Section, HostPortKey, 0).AsInt32() is var port and > 0 ? port : null;
		set => UserData.SetValue(Section, HostPortKey, value ?? 0);
	}

	public static HostEndPoint? JoinEndPoint
	{
		get
		{
			var text = UserData.GetValue(Section, JoinEndPointKey, string.Empty).AsString();
			return HostEndPoint.TryParse(text, out var endPoint) ? endPoint : null;
		}
		set => UserData.SetValue(Section, JoinEndPointKey, value?.ToString() ?? string.Empty);
	}

	public static bool IsJoinByCode
	{
		get => UserData.GetValue(Section, IsJoinByCodeKey, false).AsBool();
		set => UserData.SetValue(Section, IsJoinByCodeKey, value);
	}
}
