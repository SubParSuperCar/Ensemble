using System.Text.Json;
using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Saving;
using EnsembleRoot.Saving.SerDes;

namespace EnsembleRoot.Scripts.Plots;

/// <summary>
///     Saved creations, as files in <see cref="CreationsDir" />: JSON when named *.json, binary otherwise. Saving over
///     a file keeps the old one as a backup.
/// </summary>
public static class CreationSaves
{
	public const string BinaryExtension = ".ensb";

	private const string JsonExtension = ".json";
	private const string BackupSuffix = ".bak";

	// castle.ensb -> castle.bak.ensb, so the backup keeps its format's extension and loads like any save
	public static string GetBackupPath(string path) =>
		Path.ChangeExtension(path, null) + BackupSuffix + Path.GetExtension(path);

	/// <summary>
	///     Saves a plot's instances as they are now, returning how many, and the backup's path if a save was replaced.
	///     Key derivation and compression take a while, so they run off the main thread.
	/// </summary>
	public static async Task<(int Count, string? BackupPath)> SaveAsync(
		GdPlot plot, string path, SaveOptions options, CancellationToken cancellationToken)
	{
		var data = SaveConverter.ToSaveData(plot.Instances.Source, GTimeProvider);
		var backupPath = File.Exists(path) ? GetBackupPath(path) : null;

		await Task.Run(
			() =>
			{
				Directory.CreateDirectory(Path.GetDirectoryName(path)!);

				// A copy, so a failed save still leaves the original in place
				if (backupPath is not null)
					File.Copy(path, backupPath, true);

				GetSerializer(path).Save(path, data, options);
			},
			cancellationToken).ConfigureAwait(false);

		return (data.Instances.Count, backupPath);
	}

	public static Task<CreationSaveData> LoadAsync(
		string path, string? password, CancellationToken cancellationToken) =>
		Task.Run(() => GetSerializer(path).Load(path, new LoadOptions { Password = password }), cancellationToken);

	/// <summary>Gets whether an exception comes from a bad file, password, or option, as opposed to a bug.</summary>
	public static bool IsSaveError(Exception exception) =>
		exception is IOException or UnauthorizedAccessException or InvalidDataException or JsonException
			or ArgumentException;

	private static ISaveSerializer GetSerializer(string path) =>
		string.Equals(Path.GetExtension(path), JsonExtension, StringComparison.OrdinalIgnoreCase)
			? new JsonSaveSerializer()
			: new BinarySaveSerializer();
}
