using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class NavigatorServiceTests
{
	[Fact]
	public void GoBack_RecreatesThePreviousPage()
	{
		var navigator = CreateNavigator();

		navigator.GoTo<FirstPage>();
		var first = navigator.Current;

		navigator.GoTo<SecondPage>();
		navigator.GoBack();

		Assert.IsType<FirstPage>(navigator.Current);
		Assert.NotSame(first, navigator.Current);
		Assert.False(navigator.CanGoBack);
	}

	[Fact]
	public void GoTo_DisposesThePageLeft()
	{
		var navigator = CreateNavigator();

		navigator.GoTo<FirstPage>();
		var first = (FirstPage)navigator.Current!;

		navigator.GoTo<SecondPage>();

		Assert.True(first.IsDisposed);
		Assert.True(navigator.CanGoBack);
	}

	[Fact]
	public void GoTo_SkipsExcludedPagesInHistory()
	{
		var navigator = CreateNavigator();

		navigator.GoTo<FirstPage>();
		navigator.GoTo<SecondPage>(true);
		navigator.GoTo<ThirdPage>();
		navigator.GoBack();

		Assert.IsType<FirstPage>(navigator.Current);
	}

	[Fact]
	public void GoTo_IgnoresTheCurrentPage()
	{
		var navigator = CreateNavigator();

		navigator.GoTo<FirstPage>();
		var first = navigator.Current;

		navigator.GoTo<FirstPage>();

		Assert.Same(first, navigator.Current);
		Assert.False(navigator.CanGoBack);
	}

	private static NavigatorService CreateNavigator() => new(new ServiceCollection().BuildServiceProvider());

	public sealed class FirstPage : ViewModelBase
	{
		public bool IsDisposed { get; private set; }

		protected override void OnDispose() => IsDisposed = true;
	}

	public sealed class SecondPage : ViewModelBase;

	public sealed class ThirdPage : ViewModelBase;
}
