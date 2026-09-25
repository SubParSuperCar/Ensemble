using Avalonia.Styling;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EnsembleRoot.Ui.Impl.Messages;

public class SetUiThemeMessage(ThemeVariant theme) : ValueChangedMessage<ThemeVariant>(theme);
