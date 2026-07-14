namespace Root.Ui.Impl.Abstractions;

public abstract class DisposableObject : IDisposable
{
	public void Dispose()
	{
		OnDispose();
		GC.SuppressFinalize(this);
	}

	protected virtual void OnDispose() { }
}
