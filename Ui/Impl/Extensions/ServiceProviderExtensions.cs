using System.Diagnostics.CodeAnalysis;
using EnsembleRoot.Ui.Impl.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EnsembleRoot.Ui.Impl.Extensions;

public static class ServiceProviderExtensions
{
	extension(IServiceProvider services)
	{
		/// <summary>
		///     Creates a view model with its dependencies injected but untracked by the container, which would otherwise
		///     hold every disposable transient until its scope ends. Its owner alone disposes it.
		/// </summary>
		public TViewModel Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			TViewModel>() where TViewModel : ViewModelBase =>
			ActivatorUtilities.CreateInstance<TViewModel>(services);
	}
}
