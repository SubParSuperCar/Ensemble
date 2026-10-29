using System.Diagnostics;
using System.Text.Json;
using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.Common.Interop;
using EnsembleRoot.GdCore.Utils;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Saving;
using EnsembleRoot.Saving.SerDes;
using EnsembleRoot.Sessions;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using Lua;
using Serilog;
using TinyDialogsNet;

// ReSharper disable ExceptionLoggedAsTextProblem

namespace EnsembleRoot.Execution;

// Creations are files in CreationsDir: JSON when named *.json, binary otherwise (.ensb if no extension is given)
public static partial class LuaExecutor
{
	private const string BinaryExtension = ".ensb";
	private const string JsonExtension = ".json";
	private const string BackupSuffix = ".bak";
	private const string DeletionTitle = "Permanently Delete Saved Creation";

	private const string PlotIdKey = "plot_id";
	private const string CompressionKey = "compression";
	private const string LevelKey = "level";
	private const string ChecksumKey = "checksum";

	private static readonly string[] SaveKeys = [PlotIdKey, CompressionKey, LevelKey, PasswordKey, ChecksumKey];

	private static ValueTask<int> dmp_saves(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var (path, files) = ListFiles(CreationsDir, "*");
		Log.Information("Saved creations in {Path}:\n{Files}", path, files);

		return context.ReturnNothing();
	}

	private static async ValueTask<int> load_creation(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (GetEditablePlot(nameof(load_creation)) is not var (plot, _))
			return context.Return();

		var name = context.GetArgument<string>(0);
		var data = await LoadCreationAsync(name, context.GetArgumentOrDefault<string?>(1), cancellationToken)
			.ConfigureAwait(true);

		if (data is null)
			return context.Return();

		var (specs, skippedInstances, skippedValues) = ToSpecs(data);
		var batches = specs.Chunk(AddInstancesAction.MaxCount).Select(static batch => new AddInstancesAction(batch));
		var countBefore = plot.Instances.Count;
		var tokenCost = 0;

		foreach (var action in batches)
		{
			// The host's own requests skip the rate limiter; clients keep under it, so no batch is dropped
			if (tokenCost > 0)
				await DelayAsync(SessionManager.GetRefillTime(tokenCost), cancellationToken).ConfigureAwait(true);

			// By identity, since a later session's plot with the same ID is another object
			if (!ReferenceEquals(LocalPlot, plot) || plot.IsSpawned)
			{
				Log.Warning("Stopped loading {Name}: plot {PlotId} is no longer editable", name, plot.Id);
				return context.Return();
			}

			action.Submit();
			tokenCost = GSessionManager.IsServer ? 0 : AddInstancesAction.GetTokenCost(action.ToPayload(), default);
		}

		if (GSessionManager.IsServer)
			Log.Information(
				"Loaded {Name} into plot {PlotId}: placed {Placed} of {Count} instance(s), skipped " +
				"{SkippedInstances} instance(s) and {SkippedValues} property value(s)",
				name, plot.Id, plot.Instances.Count - countBefore, specs.Count, skippedInstances, skippedValues);
		else
			Log.Information(
				"Loaded {Name} into plot {PlotId}: submitted {Count} instance(s), skipped " +
				"{SkippedInstances} instance(s) and {SkippedValues} property value(s)",
				name, plot.Id, specs.Count, skippedInstances, skippedValues);

		return context.Return();
	}

	private static async ValueTask<int> loc_load_creation(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (FindDirectPlot(context.GetArgument<int>(0), nameof(loc_load_creation)) is not { } plot)
			return context.Return();

		var name = context.GetArgument<string>(1);
		var data = await LoadCreationAsync(name, context.GetArgumentOrDefault<string?>(2), cancellationToken)
			.ConfigureAwait(true);

		// The session may have changed while loading
		if (data is null || !ReferenceEquals(GPlots.GetPlot(plot.Id), plot))
			return context.Return();

		var countBefore = plot.Instances.Count;
		var (skippedInstances, skippedValues) = SaveConverter.FromSaveData(plot.Instances.Source, data);

		Log.Information(
			"Loaded {Name} into plot {PlotId}: added {Added} instance(s), skipped {SkippedInstances} instance(s) and " +
			"{SkippedValues} property value(s)",
			name, plot.Id, plot.Instances.Count - countBefore, skippedInstances, skippedValues);

		return context.Return();
	}

	private static async ValueTask<int> save_creation(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var name = context.GetArgument<string>(0);
		var table = context.GetArgumentOrDefault<LuaTable?>(1);

		if (
			GetCreationPath(name) is not { } path || !TryReadSaveOptions(table, out var plotId, out var options) ||
			FindPlot(plotId, nameof(save_creation)) is not { } plot)
			return context.Return();

		if (plot.Instances.Count is 0)
		{
			Log.Error("Plot {PlotId} has no instances to save", plot.Id);
			return context.Return();
		}

		var data = SaveConverter.ToSaveData(plot.Instances.Source, GTimeProvider);
		var serializer = GetSerializer(path);
		var backupPath = File.Exists(path) ? GetBackupPath(path) : null;
		var stopwatch = Stopwatch.StartNew();

		// Password key derivation and compression take a while, so they run off the main thread
		PauseBudget();

		try
		{
			await Task.Run(
				() =>
				{
					Directory.CreateDirectory(Path.GetDirectoryName(path)!);

					// A copy, so a failed save still leaves the original in place
					if (backupPath is not null)
						File.Copy(path, backupPath, true);

					serializer.Save(path, data, options);
				},
				cancellationToken).ConfigureAwait(true);
		}
		catch (Exception exception) when (IsSaveError(exception))
		{
			Log.Error("Failed to save {Name}: {Error}", name, exception.Message);
			return context.Return();
		}
		finally
		{
			ResumeBudget();
		}

		stopwatch.Stop();
		Log.Information(
			"Saved {Count} instance(s) from plot {PlotId} to {Path} in {ElapsedMs:F3} ms",
			data.Instances.Count, plot.Id, path, stopwatch.Elapsed.TotalMilliseconds);

		if (backupPath is not null)
			Log.Warning("Overwrote {Name}; the previous version is now {Backup}", name, Path.GetFileName(backupPath));

		return context.Return();
	}

	private static async ValueTask<int> rm_creation(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var name = context.GetArgument<string>(0);
		var isPermanent = context.GetArgumentOrDefault(1, false);

		if (GetCreationPath(name) is not { } path)
			return context.Return();

		if (!File.Exists(path))
		{
			Log.Error("No saved creation named {Name} (see {Function}())", name, nameof(dmp_saves));
			return context.Return();
		}

		var backupPath = GetBackupPath(path);
		var hasBackup = File.Exists(backupPath);

		var isRemoved = isPermanent
			? await DeletePermanentlyAsync(path, hasBackup, cancellationToken).ConfigureAwait(true)
			: MoveToTrash(path);

		if (isRemoved && hasBackup)
			Log.Information("Its backup {Backup} remains", Path.GetFileName(backupPath));

		return context.Return();
	}

	// Reversible from the OS's trash, so it needs no confirmation
	private static bool MoveToTrash(string path)
	{
		var fileName = Path.GetFileName(path);
		var result = OS.MoveToTrash(path);

		if (result is not Error.Ok)
		{
			Log.Error("Failed to move {File} to the trash: {Error}", fileName, result);
			return false;
		}

		Log.Information("Moved {File} to the trash", fileName);
		return true;
	}

	private static async Task<bool> DeletePermanentlyAsync(
		string path, bool hasBackup, CancellationToken cancellationToken)
	{
		var fileName = Path.GetFileName(path);
		var question = $"Permanently delete the saved creation {fileName}?\n\nThis cannot be undone";

		var prompt = hasBackup
			? $"{question}, though its backup {Path.GetFileName(GetBackupPath(path))} stays."
			: $"{question}.";

		bool isConfirmed;

		try
		{
			isConfirmed = await ConfirmAsync(DeletionTitle, prompt, cancellationToken).ConfigureAwait(true);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			Log.Information("Kept {File}; its dialog no longer does anything", fileName);
			throw;
		}

		if (!isConfirmed || !File.Exists(path))
		{
			Log.Information("Kept {File}", fileName);
			return false;
		}

		try
		{
			File.Delete(path);
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			Log.Error("Failed to delete {File}: {Error}", fileName, exception.Message);
			return false;
		}

		Log.Information("Permanently deleted {File}", fileName);
		return true;
	}

	private static async Task<CreationSaveData?> LoadCreationAsync(
		string name, string? password, CancellationToken cancellationToken)
	{
		if (GetCreationPath(name) is not { } path)
			return null;

		if (!File.Exists(path))
		{
			Log.Error("No saved creation named {Name} (see {Function}())", name, nameof(dmp_saves));
			return null;
		}

		var serializer = GetSerializer(path);
		var options = new LoadOptions { Password = password };

		PauseBudget();

		try
		{
			return await Task.Run(() => serializer.Load(path, options), cancellationToken).ConfigureAwait(true);
		}
		catch (Exception exception) when (IsSaveError(exception))
		{
			Log.Error("Failed to load {Name}: {Error}", name, exception.Message);
			return null;
		}
		finally
		{
			ResumeBudget();
		}
	}

	// Drops what AddInstancesAction would reject a whole batch for (unknown assets, invalid values), like loc_ loads
	private static (List<InstanceSpec> Specs, int SkippedInstances, int SkippedValues) ToSpecs(CreationSaveData data)
	{
		var specs = new List<InstanceSpec>();
		var (skippedInstances, skippedValues) = (0, 0);

		foreach (var instance in data.Instances)
		{
			if (GAssets.GetAsset(instance.AssetId) is not { } asset)
			{
				skippedInstances++;
				continue;
			}

			var current = asset.Source.Properties;
			var properties = new Dictionary();

			foreach (var (key, value) in instance.Properties ?? [])
				if (current.TryGetValue(key, out var existing) && PropertyRules.Check(key, value, existing) is null)
					properties[key] = value.ToGodot();
				else
					skippedValues++;

			var rotation = instance.Rotation.ToGodot();

			if (!rotation.IsNormalized())
				rotation = rotation.Normalized();

			specs.Add(new InstanceSpec(instance.AssetId, instance.Position.ToGodot(), rotation, properties));
		}

		return (specs, skippedInstances, skippedValues);
	}

	private static bool TryReadSaveOptions(LuaTable? table, out int? plotId, out SaveOptions options)
	{
		plotId = null;
		options = SaveOptions.Default;

		foreach (var (key, value) in table ?? [])
		{
			var text = value.Type is LuaValueType.String ? value.Read<string>() : null;
			var number = ToInt(value);

			switch (key.Type is LuaValueType.String ? key.Read<string>() : null)
			{
				case PlotIdKey when number is { } id:
					plotId = id;
					break;

				case CompressionKey when ToCompression(text) is { } compression:
					options = options with { Compression = compression };
					break;

				case LevelKey when number is { } level:
					options = options with { CompressionLevel = level };
					break;

				case PasswordKey when text is { Length: > 0 }:
					options = options with { Encryption = new SaveEncryption.Password(text) };
					break;

				case ChecksumKey when value.Type is LuaValueType.Boolean:
					options = options with { UseChecksum = value.Read<bool>() };
					break;

				default:
					LogInvalidOption(key, value, SaveKeys);
					return false;
			}
		}

		return true;
	}

	private static string? GetCreationPath(string name) => GetUserFilePath(CreationsDir, name, BinaryExtension);

	// TryParse also takes any number, defined or not
	private static CompressionType? ToCompression(string? text) =>
		Enum.TryParse(text, true, out CompressionType compression) && Enum.IsDefined(compression) ? compression : null;

	/// <summary>Asks a Yes / No question, true only for an explicit Yes.</summary>
	/// <remarks>
	///     The dialog blocks its thread, so it runs on the thread pool. A dialog that can't open reads as No, and
	///     canceling stops the wait, not the dialog.
	/// </remarks>
	private static async Task<bool> ConfirmAsync(string title, string prompt, CancellationToken cancellationToken)
	{
		PauseBudget();

		try
		{
			return await Task.Run(() => Confirm(title, prompt), CancellationToken.None)
				.WaitAsync(cancellationToken)
				.ConfigureAwait(true);
		}
		finally
		{
			ResumeBudget();
		}
	}

	private static bool Confirm(string title, string prompt)
	{
		try
		{
			return YesNoDialog.Ask(title, prompt, MessageBoxIconType.Warning, false);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to show the {Title} dialog", title);
			return false;
		}
	}

	// castle.ensb -> castle.bak.ensb, so the backup keeps its format's extension and loads like any save
	private static string GetBackupPath(string path) =>
		Path.ChangeExtension(path, null) + BackupSuffix + Path.GetExtension(path);

	private static ISaveSerializer GetSerializer(string path) =>
		string.Equals(Path.GetExtension(path), JsonExtension, StringComparison.OrdinalIgnoreCase)
			? new JsonSaveSerializer()
			: new BinarySaveSerializer();

	// What a bad file, password, or option causes, as opposed to bugs
	private static bool IsSaveError(Exception exception) =>
		exception is IOException or UnauthorizedAccessException or InvalidDataException or JsonException
			or ArgumentException;
}
