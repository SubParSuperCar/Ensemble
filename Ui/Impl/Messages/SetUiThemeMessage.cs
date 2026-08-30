using Avalonia.Styling;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Root.Ui.Impl.Messages;

public class SetUiThemeMessage(ThemeVariant theme) : ValueChangedMessage<ThemeVariant>(theme);
