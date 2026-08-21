namespace Root.SessionManager.Actions;

public interface INetworkActionHandler<in TAction> where TAction : INetworkAction<TAction>
{
	ActionValidation Validate(TAction action, int sourcePeerId);

	void Apply(TAction action, int sourcePeerId);
}
