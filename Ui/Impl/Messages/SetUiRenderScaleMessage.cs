using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Root.Ui.Impl.Messages;

public class SetUiRenderScaleMessage(double scale) : ValueChangedMessage<double>(scale);
