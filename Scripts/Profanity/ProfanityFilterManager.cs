using EnsembleRoot.Autoloading;
using EnsembleRoot.Common.Text;
using EnsembleRoot.Common.Utils;
using Godot;

namespace EnsembleRoot.Scripts.Profanity;

/// <summary>
///     Keeps whether the current session's profanity filter is on: the host's choice to mask profane words in chat
///     messages and give players with inappropriate display names the default name instead, through
///     <see cref="ProfanityFilter" />.
/// </summary>
/// <remarks>
///     Changes arrive as network actions, and late joiners receive the state as a snapshot (see Replication). When
///     hosting, the filter starts as <see cref="Override" />, if set, or <see cref="IsPreferred" />.
/// </remarks>
[GlobalClass]
[Autoload(Order = AutoloadOrder.Standard + 1, FailurePolicy = AutoloadFailurePolicy.LogAndContinue)]
public partial class ProfanityFilterManager : Node, IAutoload
{
	[Signal]
	public delegate void EnabledChangedEventHandler(bool isEnabled);

	private const string Section = "profanity_filter";
	private const string IsPreferredKey = "enabled";

	public static ProfanityFilterManager? Instance { get; private set; }

	public bool IsEnabled { get; private set; }

	/// <summary>Whether to turn the profanity filter on when hosting. Persisted.</summary>
	public static bool IsPreferred
	{
		get => UserData.GetValue(Section, IsPreferredKey, true).AsBool();
		set => UserData.SetValue(Section, IsPreferredKey, value);
	}

	/// <summary>Overrides <see cref="IsPreferred" /> without persisting it (e.g., from user args).</summary>
	public static bool? Override { get; set; }

	public void Initialize()
	{
		Instance = this;

		GSessionManager.SessionStarted += OnSessionStarted;
		GSessionManager.SessionStopped += OnSessionStopped;
		GSessionManager.DisplayNameFilter = IsDisplayNameAllowed;
	}

	public override void _ExitTree()
	{
		GSessionManager.SessionStarted -= OnSessionStarted;
		GSessionManager.SessionStopped -= OnSessionStopped;
		GSessionManager.DisplayNameFilter = null;

		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	/// <summary>Masks the profanity in <paramref name="text" /> while the profanity filter is on.</summary>
	public string Filter(string text) => IsEnabled ? ProfanityFilter.Censor(text) : text;

	/// <returns>Whether the state changed.</returns>
	internal bool SetEnabled(bool isEnabled)
	{
		if (IsEnabled == isEnabled)
			return false;

		IsEnabled = isEnabled;
		EmitSignal(SignalName.EnabledChanged, isEnabled);

		return true;
	}

	private bool IsDisplayNameAllowed(string displayName) =>
		string.Equals(Filter(displayName), displayName, StringComparison.Ordinal);

	// Preloaded even while off, since the host can turn it on mid-session
	private void OnSessionStarted()
	{
		if (!GSessionManager.IsServer)
			return;

		SetEnabled(Override ?? IsPreferred);
		ProfanityFilter.Preload();
	}

	private void OnSessionStopped() => SetEnabled(false);
}
