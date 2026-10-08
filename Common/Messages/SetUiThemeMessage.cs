using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EnsembleRoot.Common.Messages;

public sealed class SetUiThemeMessage(bool? useDarkTheme) : ValueChangedMessage<bool?>(useDarkTheme);
