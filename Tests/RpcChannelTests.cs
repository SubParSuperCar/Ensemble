using System.Reflection;
using EnsembleRoot.Scripts.Plots;
using Godot;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class RpcChannelTests
{
	// Anything may call these, so each must check its sender, its shape, and its rate before acting (see TECHNICAL.md).
	// A new one belongs here only once it does
	private static readonly string[] AnyPeerRpcs =
	[
		"PlayerHandle.RpcReplicate",
		"PlotHandle.RpcReceiveAssetStates",
		"PlotHandle.RpcReceiveCreation",
		"PlotHandle.RpcReceiveInput",
		"SessionManager.RpcRequestAction",
		"SessionManager.RpcRequestRegister",
		"SessionManager.RpcRequestSnapshots"
	];

	[Fact]
	public void AnyPeerRpcs_AreAllAccountedFor()
	{
		var rpcs = typeof(PlotHandle).Assembly.GetTypes()
			.SelectMany(static type => type.GetMethods(
				BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			.Where(static method => method.GetCustomAttribute<RpcAttribute>()?.Mode is MultiplayerApi.RpcMode.AnyPeer)
			.Select(static method => $"{method.DeclaringType!.Name}.{method.Name}")
			.Order(StringComparer.Ordinal);

		Assert.Equal(AnyPeerRpcs, rpcs, StringComparer.Ordinal);
	}
}
