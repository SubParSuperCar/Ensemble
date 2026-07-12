using Microsoft.Extensions.DependencyInjection;
using Root.Ui.Impl.Abstractions;

namespace Root.Ui.Impl.Extensions;

// Provides extensions for scanning the assembly and registering services (actual services and ViewModels)
// Each object gets one lifetime depending on which are defined
public static class ServiceCollectionExtensions
{
	public static void AddServices(this IServiceCollection services) =>
		services.Scan(scan => scan
			.FromAssembliesOf(typeof(ServiceCollectionExtensions))
			.AddClasses(c => c
				.AssignableTo<ITransientObject>()
				.Where(t =>
					!typeof(IScopedObject).IsAssignableFrom(t) &&
					!typeof(ISingletonObject).IsAssignableFrom(t)))
			.AsSelfWithInterfaces()
			.WithTransientLifetime()
			.AddClasses(c => c
				.AssignableTo<IScopedObject>()
				.Where(t =>
					!typeof(ISingletonObject).IsAssignableFrom(t)))
			.AsSelfWithInterfaces()
			.WithScopedLifetime()
			.AddClasses(c => c
				.AssignableTo<ISingletonObject>())
			.AsSelfWithInterfaces()
			.WithSingletonLifetime());
}
