using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;
using ServiceScan.SourceGenerator;

namespace Root.Ui.Impl.Extensions;

public static partial class ServiceCollectionExtensions
{
	// Automatically looks up and registers services at compile time. Super clean and fast and leagues better
	// than manually enumerating everything. I love this.
	[GenerateServiceRegistrations(
		AssignableTo = typeof(ITransientObject),
		Lifetime = ServiceLifetime.Transient,
		AsSelf = true,
		AsImplementedInterfaces = true)]
	[GenerateServiceRegistrations(
		AssignableTo = typeof(IScopedObject),
		Lifetime = ServiceLifetime.Scoped,
		AsSelf = true,
		AsImplementedInterfaces = true)]
	[GenerateServiceRegistrations(
		AssignableTo = typeof(ISingletonObject),
		Lifetime = ServiceLifetime.Singleton,
		AsSelf = true,
		AsImplementedInterfaces = true)]
	[GenerateServiceRegistrations(
		AssignableTo = typeof(IViewFor<>),
		Lifetime = ServiceLifetime.Transient)]
	// ReSharper disable once UnusedMethodReturnValue.Global
	public static partial IServiceCollection AddServices(this IServiceCollection services);
}
