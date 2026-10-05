namespace EnsembleRoot.Ui.Impl.Abstractions;

public abstract class DisposableObject : IDisposable
{
	private bool _isDisposed;

	public void Dispose()
	{
		if (_isDisposed)
			return;

		_isDisposed = true;

		OnDispose();
		GC.SuppressFinalize(this);
	}

	protected virtual void OnDispose() { }
}
