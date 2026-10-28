using EnsembleRoot.Ui.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using ServiceScan.SourceGenerator;

namespace EnsembleRoot.Ui.Extensions;

public static partial class ServiceCollectionExtensions
{
	[GenerateServiceRegistrations(
		AssignableTo = typeof(ITransientObject), Lifetime = ServiceLifetime.Transient, AsSelf = true)]
	[GenerateServiceRegistrations(
		AssignableTo = typeof(IScopedObject), Lifetime = ServiceLifetime.Scoped, AsSelf = true)]
	[GenerateServiceRegistrations(
		AssignableTo = typeof(ISingletonObject), Lifetime = ServiceLifetime.Singleton, AsSelf = true)]
	[GenerateServiceRegistrations(AssignableTo = typeof(IViewFor<>), Lifetime = ServiceLifetime.Transient)]
	public static partial void AddServices(this IServiceCollection services);
}
