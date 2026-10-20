namespace EnsembleRoot.Sessions;

public partial class SessionManager
{
	private const double TrafficInterval = 1d;

	private double _sinceTrafficSample;

	/// <summary>The bytes a second the session's transport received over the last second, zero offline.</summary>
	public double ReceivedBytesPerSecond { get; private set; }

	/// <summary>The bytes a second the session's transport sent over the last second, zero offline.</summary>
	public double SentBytesPerSecond { get; private set; }

	private void UpdateTraffic(double delta)
	{
		if (_session is not { } session)
		{
			_sinceTrafficSample = 0d;
			ReceivedBytesPerSecond = SentBytesPerSecond = 0d;

			return;
		}

		_sinceTrafficSample += delta;
		if (_sinceTrafficSample < TrafficInterval)
			return;

		var (received, sent) = session.PopTraffic();

		ReceivedBytesPerSecond = received / _sinceTrafficSample;
		SentBytesPerSecond = sent / _sinceTrafficSample;
		_sinceTrafficSample = 0d;
	}
}
