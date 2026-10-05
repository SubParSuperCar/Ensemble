using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EnsembleRoot.Common.Messages;

public class SetUiThemeMessage(bool? useDarkTheme) : ValueChangedMessage<bool?>(useDarkTheme);
