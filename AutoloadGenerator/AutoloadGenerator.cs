using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AutoloadGenerator;

[Generator]
public sealed class AutoloadGenerator : IIncrementalGenerator
{
	private const string AttributeMetadataName = "EnsembleRoot.Autoloading.AutoloadAttribute";

	private const string ScopePropertyName = "Scope";
	private const string OrderPropertyName = "Order";
	private const string FailurePolicyPropertyName = "FailurePolicy";

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var autoloads = context.SyntaxProvider.ForAttributeWithMetadataName(
			AttributeMetadataName,
			static (_, _) => true,
			static (target, _) =>
			{
				var attribute = target.Attributes[0];

				return (
					TypeName: target.TargetSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
					Scope: GetScope(attribute),
					Order: GetOrder(attribute),
					FailurePolicy: GetFailurePolicy(attribute));
			});

		context.RegisterSourceOutput(autoloads.Collect(), Generate);
	}

	private static void Generate(
		SourceProductionContext context,
		ImmutableArray<(string TypeName, string Scope, string Order, string FailurePolicy)> autoloads)
	{
		var source = new StringBuilder();

		source.AppendLine(
			"""
			namespace EnsembleRoot.Autoloading;

			public static partial class AutoloadRegistry
			{
				public static partial AutoloadDefinition[] GetAll() =>
				[
			""");

		foreach (var autoload in autoloads.OrderBy(static autoload => autoload.TypeName, StringComparer.Ordinal))
			source.AppendLine(
				"\t\t\t\tnew(" +
				$"typeof({autoload.TypeName}), " +
				$"{autoload.Scope}, " +
				$"{autoload.Order}, " +
				$"{autoload.FailurePolicy}, " +
				$"static () => new {autoload.TypeName}()),");

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
			: "AutoloadScope.RegularClient | AutoloadScope.HeadlessServer";

	private static string GetOrder(AttributeData attribute) =>
		TryGetNamedArgument(attribute, OrderPropertyName, out var value)
			? Convert.ToSByte(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
			: "AutoloadOrder.Standard";

	private static string GetFailurePolicy(AttributeData attribute) =>
		TryGetNamedArgument(attribute, FailurePolicyPropertyName, out var value)
			? $"(AutoloadFailurePolicy){Convert.ToInt32(value, CultureInfo.InvariantCulture)}"
			: "AutoloadFailurePolicy.AskUser";

	private static bool TryGetNamedArgument(AttributeData attribute, string name, out object? value)
	{
		// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
		foreach (var argument in attribute.NamedArguments)
			if (string.Equals(argument.Key, name, StringComparison.Ordinal))
			{
				value = argument.Value.Value;
				return true;
			}

		value = null;
		return false;
	}
}
