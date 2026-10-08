using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Messaging.Messages;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace EnsembleRoot.Ui.Impl.Messages;

public sealed class UiProcessMessage(UiProcessData data) : ValueChangedMessage<UiProcessData>(data);

[StructLayout(LayoutKind.Auto)]
public readonly record struct UiProcessData(double SinceLastUiProcess, double SinceLastGodotProcess);
