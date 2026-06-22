using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace Estragonia;

internal sealed class BclStorageFolder(DirectoryInfo directoryInfo) : IStorageBookmarkFolder
{
	public DirectoryInfo DirectoryInfo { get; } = directoryInfo;

	public string Name => DirectoryInfo.Name;

	public bool CanBookmark => true;

	public Uri Path => field ??= BuildPath();

	public Task<StorageItemProperties> GetBasicPropertiesAsync() =>
		Task.FromResult(new StorageItemProperties(
			null,
			DirectoryInfo.CreationTimeUtc,
			DirectoryInfo.LastAccessTimeUtc
		));

	public Task<IStorageFolder?> GetParentAsync()
	{
		var storageFolder = DirectoryInfo.Parent is { } directory ? new BclStorageFolder(directory) : null;
		return Task.FromResult<IStorageFolder?>(storageFolder);
	}

	public IAsyncEnumerable<IStorageItem> GetItemsAsync() =>
		DirectoryInfo.EnumerateDirectories()
			.Select(IStorageItem (d) => new BclStorageFolder(d))
			.Concat(DirectoryInfo.EnumerateFiles().Select(f => new BclStorageFile(f)))
			.AsAsyncEnumerable();

	public Task<IStorageFolder?> GetFolderAsync(string name)
	{
		var directory = DirectoryInfo.EnumerateDirectories().FirstOrDefault(d => d.Name == name);
		return directory is null
			? Task.FromResult<IStorageFolder?>(null)
			: Task.FromResult<IStorageFolder?>(new BclStorageFolder(directory));
	}

	public Task<IStorageFile?> GetFileAsync(string name)
	{
		var file = DirectoryInfo.EnumerateFiles().FirstOrDefault(f => f.Name == name);
		return file is null
			? Task.FromResult<IStorageFile?>(null)
			: Task.FromResult<IStorageFile?>(new BclStorageFile(file));
	}

	public Task<string?> SaveBookmarkAsync() => Task.FromResult<string?>(DirectoryInfo.FullName);

	public Task ReleaseBookmarkAsync() => Task.CompletedTask;

	public void Dispose()
	{
	}

	public Task DeleteAsync()
	{
		DirectoryInfo.Delete(true);
		return Task.CompletedTask;
	}

	public Task<IStorageItem?> MoveAsync(IStorageFolder destination)
	{
		if (destination is not BclStorageFolder storageFolder) return Task.FromResult<IStorageItem?>(null);
		var newPath = System.IO.Path.Combine(storageFolder.DirectoryInfo.FullName, DirectoryInfo.Name);
		DirectoryInfo.MoveTo(newPath);

		return Task.FromResult<IStorageItem?>(new BclStorageFolder(new DirectoryInfo(newPath)));
	}

	public Task<IStorageFile?> CreateFileAsync(string name)
	{
		var fileName = System.IO.Path.Combine(DirectoryInfo.FullName, name);
		var newFile = new FileInfo(fileName);

		using var stream = newFile.Create();

		return Task.FromResult<IStorageFile?>(new BclStorageFile(newFile));
	}

	public Task<IStorageFolder?> CreateFolderAsync(string name)
	{
		var newFolder = DirectoryInfo.CreateSubdirectory(name);

		return Task.FromResult<IStorageFolder?>(new BclStorageFolder(newFolder));
	}

	private Uri BuildPath()
	{
		try
		{
			var builder = new UriBuilder
			{
				Scheme = Uri.UriSchemeFile,
				Host = string.Empty,
				Path = DirectoryInfo.FullName
			};
			return builder.Uri;
		}
		catch (SecurityException)
		{
			return new Uri(DirectoryInfo.Name, UriKind.Relative);
		}
	}
}
