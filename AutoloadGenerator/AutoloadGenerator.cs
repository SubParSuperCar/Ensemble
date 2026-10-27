using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AutoloadGenerator;

[Generator]
public sealed class AutoloadGenerator : IIncrementalGenerator
{
	private const string AttributeMetadataName = "EnsembleRoot.Autoloading.AutoloadAttribute";
	private const string HintName = "AutoloadRegistry.g.cs";

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var autoloads = context.SyntaxProvider.ForAttributeWithMetadataName(
			AttributeMetadataName,
			static (_, _) => true,
			static (target, _) => (
				TypeName: target.TargetSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				Arguments: string.Join(
					", ",
					target.Attributes[0].NamedArguments.Select(static argument =>
						$"{argument.Key} = {argument.Value.ToCSharpString()}"))));

		context.RegisterSourceOutput(autoloads.Collect(), Generate);
	}

	private static void Generate(
		SourceProductionContext context, ImmutableArray<(string TypeName, string Arguments)> autoloads)
	{
		var source = string.Join(
			"\n",
			[
				"""
				namespace EnsembleRoot.Autoloading;

				public static partial class AutoloadRegistry
				{
					public static partial AutoloadDefinition[] GetAll() =>
					[
				""",
				.. autoloads
					.OrderBy(static autoload => autoload.TypeName, StringComparer.Ordinal)
					.Select(static autoload =>
						$"\t\tnew(typeof({autoload.TypeName}), new() {{ {autoload.Arguments} }}, " +
						$"static () => new {autoload.TypeName}()),"),
				"""
					];
				}

				"""
			]);

		context.AddSource(HintName, SourceText.From(source, Encoding.UTF8));
	}
}
