using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Godot;

namespace Estragonia;

/// <summary>An implementation of <see cref="ILauncher" /> that uses <see cref="OS.ShellOpen" />.</summary>
internal sealed class GodotLauncher : ILauncher
{
	public Task<bool> LaunchUriAsync(Uri uri) =>
		Task.FromResult(uri.IsAbsoluteUri && OS.ShellOpen(uri.AbsoluteUri) is Error.Ok);

	public Task<bool> LaunchFileAsync(IStorageItem storageItem) =>
		Task.FromResult(storageItem.TryGetLocalPath() is { } path && OS.ShellOpen(path) is Error.Ok);
}
