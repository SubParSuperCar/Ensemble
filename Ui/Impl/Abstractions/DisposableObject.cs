namespace Root.Ui.Impl.Abstractions;

// Special object that lets you define a custom dispose method while always calling SuppressFinalize (less repetition)
public abstract class DisposableObject : IDisposable
{
	public void Dispose()
	{
		OnDispose();
		GC.SuppressFinalize(this);
	}

	protected virtual void OnDispose() { }
}
