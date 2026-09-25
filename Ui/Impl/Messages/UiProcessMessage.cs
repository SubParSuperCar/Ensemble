using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EnsembleRoot.Ui.Impl.Messages;

public class UiProcessMessage(UiProcessData data) : ValueChangedMessage<UiProcessData>(data);

[StructLayout(LayoutKind.Auto)]
public readonly record struct UiProcessData(double SinceLastUiProcess, double SinceLastGodotProcess);
