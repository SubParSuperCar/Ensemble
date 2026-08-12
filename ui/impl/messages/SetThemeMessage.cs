using Avalonia.Styling;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Root.Ui.Impl.Messages;

public class SetThemeMessage(ThemeVariant theme) : ValueChangedMessage<ThemeVariant>(theme);
