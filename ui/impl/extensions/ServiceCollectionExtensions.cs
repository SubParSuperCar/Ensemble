using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;
using ServiceScan.SourceGenerator;

namespace Root.Ui.Impl.Extensions;

public static partial class ServiceCollectionExtensions
{
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
	// ReSharper disable once UnusedMethodReturnValue.Global
	public static partial IServiceCollection AddServices(this IServiceCollection services);
}
