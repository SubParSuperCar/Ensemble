using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EnsembleRoot.Common.Messages;

public sealed class SetUiRenderScaleMessage(double scale) : ValueChangedMessage<double>(scale);
