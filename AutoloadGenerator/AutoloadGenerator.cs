using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AutoloadGenerator;

[Generator]
public sealed class AutoloadGenerator : IIncrementalGenerator
{
	private const string AttributeMetadataName = "Root.Autoloading.AutoloadAttribute";

	private const string ScopePropertyName = "Scope";
	private const string OrderPropertyName = "Order";
	private const string FailurePolicyPropertyName = "FailurePolicy";

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var autoloads = context.SyntaxProvider.ForAttributeWithMetadataName(
			AttributeMetadataName,
			static (_, _) => true,
			static (context, _) =>
				((INamedTypeSymbol)context.TargetSymbol, context.Attributes[0]));

		context.RegisterSourceOutput(
			autoloads.Collect(),
			static (context, autoloads) => Generate(context, autoloads));
	}

	private static void Generate(
		SourceProductionContext context,
		ImmutableArray<(INamedTypeSymbol Type, AttributeData Attribute)> autoloads)
	{
		var source = new StringBuilder();

		source.AppendLine(
			"""
			namespace Root.Autoloading;

			public static partial class AutoloadRegistry
			{
				public static partial AutoloadDefinition[] GetAll() =>
				[
			""");

		foreach (
			var (typeName, attribute) in autoloads
				.Select(static autoload =>
					(Name: autoload.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), autoload.Attribute))
				.OrderBy(static autoload => autoload.Name, StringComparer.Ordinal))
			source.AppendLine(
				"\t\t\t\tnew(" +
				$"typeof({typeName}), " +
				$"{GetScope(attribute)}, " +
				$"{GetOrder(attribute)}, " +
				$"{GetFailurePolicy(attribute)}, " +
				$"static () => new {typeName}()),");

		source.AppendLine(
			"""
				];
			}
			""");

		context.AddSource(
			"AutoloadRegistry.g.cs",
			SourceText.From(source.ToString(), Encoding.UTF8));
	}

	private static string GetScope(AttributeData attribute) =>
		TryGetNamedArgument(attribute, ScopePropertyName, out var value)
			? $"(AutoloadScope){Convert.ToInt32(value, CultureInfo.InvariantCulture)}"
			: "AutoloadScope.Client | AutoloadScope.Server";

	private static string GetOrder(AttributeData attribute) =>
		TryGetNamedArgument(attribute, OrderPropertyName, out var value)
			? Convert.ToSByte(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
			: "0";

	private static string GetFailurePolicy(AttributeData attribute) =>
		TryGetNamedArgument(attribute, FailurePolicyPropertyName, out var value)
			? $"(AutoloadFailurePolicy){Convert.ToInt32(value, CultureInfo.InvariantCulture)}"
			: "AutoloadFailurePolicy.AskUser";

	private static bool TryGetNamedArgument(AttributeData attribute, string name, out object? value)
	{
		foreach (
			var argument in attribute.NamedArguments.Where(argument =>
				string.Equals(argument.Key, name, StringComparison.Ordinal)))
		{
			value = argument.Value.Value;
			return true;
		}

		value = null;
		return false;
	}
}
