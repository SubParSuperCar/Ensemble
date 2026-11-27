using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EnsembleRoot.Common.Messages;

public sealed class SetStatsShownMessage(bool isShown) : ValueChangedMessage<bool>(isShown);
