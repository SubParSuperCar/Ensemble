using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EnsembleRoot.Ui.Impl.Messages;

public class SetUiRenderScaleMessage(double scale) : ValueChangedMessage<double>(scale);
