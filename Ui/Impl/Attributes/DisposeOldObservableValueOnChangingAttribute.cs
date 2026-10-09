using MethodBoundaryAspect.Fody.Attributes;

namespace EnsembleRoot.Ui.Impl.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
[Serializable]
public sealed class DisposeOldObservableValueOnChangingAttribute : OnMethodBoundaryAspect
{
	private const string SetterPrefix = "set_";

	public override void OnEntry(MethodExecutionArgs arg)
	{
		if (!arg.Method.Name.StartsWith(SetterPrefix, StringComparison.Ordinal))
			return;

		var property = arg.Instance.GetType().GetProperty(arg.Method.Name[SetterPrefix.Length..]);

		if (property?.GetValue(arg.Instance) is IDisposable oldValue && !Equals(oldValue, arg.Arguments[0]))
			oldValue.Dispose();
	}
}
