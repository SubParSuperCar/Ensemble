using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class NavigatorServiceTests
{
	[Fact]
	public void GoBack_RecreatesPreviousPageSkippingExcluded()
	{
		var navigator = new NavigatorService(new ServiceCollection().BuildServiceProvider());

		navigator.GoTo<FirstPage>();
		var first = (FirstPage)navigator.Current!;

		navigator.GoTo<SecondPage>(true);
		navigator.GoTo<ThirdPage>();
		navigator.GoBack();

		Assert.True(first.WasDisposed);
		Assert.IsType<FirstPage>(navigator.Current);
		Assert.NotSame(first, navigator.Current);
		Assert.False(navigator.CanGoBack);
	}

	public sealed class FirstPage : ViewModelBase
	{
		public bool WasDisposed => IsDisposed;
	}

	public sealed class SecondPage : ViewModelBase;

	public sealed class ThirdPage : ViewModelBase;
}
