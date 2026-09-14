using System;
using Avalonia.Rendering;

namespace Estragonia;

/// <summary>An <see cref="IRenderTimer" /> implementation that is only triggered manually.</summary>
internal sealed class ManualRenderTimer : IRenderTimer
{
	public Action<TimeSpan>? Tick { get; set; }

	bool IRenderTimer.RunsInBackground => false;

	public void TriggerTick(TimeSpan elapsed) => Tick?.Invoke(elapsed);
}
