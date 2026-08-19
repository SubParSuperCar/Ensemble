using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AutoloadGenerator;

[Generator]
public sealed class AutoloadGenerator : IIncrementalGenerator
{
	private const string AttributeMetadataName =
		"Root.Autoloading.AutoloadAttribute";

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

		source.AppendLine("""
		                  namespace Root.Autoloading;

		                  public static partial class AutoloadRegistry
		                  {
		                  	public static partial AutoloadDefinition[] GetAll() =>
		                  	[
		                  """);

		foreach (var (type, attribute) in autoloads)
		{
			var typeName = type.ToDisplayString(
				SymbolDisplayFormat.FullyQualifiedFormat);

			source.AppendLine(
				$"\t\t\t\tnew(" +
				$"typeof({typeName}), " +
				$"{GetScope(attribute)}, " +
				$"{GetOrder(attribute)}, " +
				$"{GetFailurePolicy(attribute)}, " +
				$"static () => new {typeName}()),");
		}

		source.AppendLine("""
		                  	];
		                  }
		                  """);

		context.AddSource(
			"AutoloadRegistry.g.cs",
			SourceText.From(source.ToString(), Encoding.UTF8));
	}

	private static string GetScope(AttributeData attribute)
	{
		foreach (var argument in attribute.NamedArguments.Where(argument => argument.Key is ScopePropertyName))
			return $"(AutoloadScopeFlag){Convert.ToInt32(argument.Value.Value)}";

		return "AutoloadScopeFlag.Client | AutoloadScopeFlag.Server";
	}

	private static string GetOrder(AttributeData attribute)
	{
		foreach (var argument in attribute.NamedArguments.Where(argument => argument.Key is OrderPropertyName))
			return Convert.ToSByte(argument.Value.Value).ToString();

		return "0";
	}

	private static string GetFailurePolicy(AttributeData attribute)
	{
		foreach (var argument in attribute.NamedArguments.Where(argument => argument.Key is FailurePolicyPropertyName))
			return $"(AutoloadFailurePolicyEnum){Convert.ToInt32(argument.Value.Value)}";

		return "AutoloadFailurePolicyEnum.AskUser";
	}
}
