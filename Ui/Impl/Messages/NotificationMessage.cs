using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Root.Ui.Impl.Messages;

public class NotificationMessage(int what) : ValueChangedMessage<int>(what);
