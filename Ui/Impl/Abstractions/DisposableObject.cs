namespace EnsembleRoot.Ui.Impl.Abstractions;

public abstract class DisposableObject : IDisposable
{
	protected bool IsDisposed { get; private set; }

	public void Dispose()
	{
		if (IsDisposed)
			return;

		IsDisposed = true;

		OnDispose();
		GC.SuppressFinalize(this);
	}

	protected virtual void OnDispose() { }
}
