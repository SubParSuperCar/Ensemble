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

#pragma warning disable IL2075
		var property = arg.Instance.GetType().GetProperty(arg.Method.Name[SetterPrefix.Length..]);
#pragma warning restore IL2075

		var oldValue = property?.GetValue(arg.Instance);
		var newValue = arg.Arguments.Length is 0 ? null : arg.Arguments[0];

		if (oldValue is IDisposable value && !Equals(oldValue, newValue))
			value.Dispose();
	}
}
