using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Root.Ui.Impl.Messages;

public class ProcessMessage(double delta) : ValueChangedMessage<double>(delta);
