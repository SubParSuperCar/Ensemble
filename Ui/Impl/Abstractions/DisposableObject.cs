namespace Root.Ui.Impl.Abstractions;

public abstract class DisposableObject : IDisposable
{
	// Abstraction so we don't have to write GC.SuppressFinalize every time. Nice boilerplate reduction.
	public void Dispose()
	{
		OnDispose();
		GC.SuppressFinalize(this);
	}

	protected virtual void OnDispose() { }
}
