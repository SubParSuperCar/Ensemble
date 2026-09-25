using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EnsembleRoot.Ui.Impl.Messages;

public class NotificationMessage(int what) : ValueChangedMessage<int>(what);
