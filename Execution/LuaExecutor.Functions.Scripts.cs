using System.Globalization;
using EnsembleRoot.Common.Utils;
using Godot;
using Lua;
using Serilog;

namespace EnsembleRoot.Execution;

// Script files live in ScriptsDir: run_script("name") runs name.lua, and require("name") loads it as a module
public static partial class LuaExecutor
{
	private const string ScriptExtension = ".lua";

	private static ValueTask<int> dmp_scripts(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var directory = new DirectoryInfo(ProjectSettings.GlobalizePath(ScriptsDir));
		IEnumerable<FileInfo> files = directory.Exists
			? directory.GetFiles($"*{ScriptExtension}").OrderBy(static file => file.Name, StringComparer.Ordinal)
			: [];

		var lines = files.Select(static file => string.Create(
			CultureInfo.InvariantCulture,
			$"{file.Name} ({ByteFormat.Humanize((ulong)file.Length)}, {file.LastWriteTime:yyyy-MM-dd HH:mm:ss})"));

		Log.Information("Scripts in {Path}:\n{Files}", directory.FullName, string.Join('\n', lines));

		return context.ReturnNothing();
	}

	/// <summary>Runs a script file in the calling script's environment and returns what it returns.</summary>
	private static async ValueTask<int> run_script(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var name = context.GetArgument<string>(0);

		if (GetUserFilePath(ScriptsDir, name, ScriptExtension) is not { } path)
			return context.Return();

		if (!File.Exists(path))
		{
			Log.Error("No script named {Name} (see {Function}())", name, nameof(dmp_scripts));
			return context.Return();
		}

		var source = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(true);

		// Like dofile: the script's results replace this call's arguments on the stack
		context.State.Stack.PopUntil(context.ReturnFrameBase);
		var chunk = context.State.Load(source, $"@{Path.GetFileName(path)}");

		return await context.State.RunAsync(chunk, cancellationToken).ConfigureAwait(true);
	}

	// Module names are plain file names without the extension, so require("utils") loads utils.lua
	private sealed class ScriptModuleLoader(string directory) : ILuaModuleLoader
	{
		public bool Exists(string moduleName) => GetPath(moduleName) is { } path && File.Exists(path);

		public async ValueTask<LuaModule> LoadAsync(string moduleName, CancellationToken cancellationToken = default)
		{
			var path = GetPath(moduleName) ?? throw new ArgumentException("Invalid module name.", nameof(moduleName));
			var source = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(true);

			return new LuaModule(moduleName, source.AsMemory());
		}

		private string? GetPath(string moduleName) =>
			moduleName.Length > 0 && moduleName.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
				? Path.Join(directory, moduleName + ScriptExtension)
				: null;
	}
}
