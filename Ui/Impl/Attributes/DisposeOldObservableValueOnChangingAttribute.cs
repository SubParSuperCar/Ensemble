using MethodBoundaryAspect.Fody.Attributes;

namespace Root.Ui.Impl.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
[Serializable]
public sealed class DisposeOldObservableValueOnChangingAttribute : OnMethodBoundaryAspect
{
	public override void OnEntry(MethodExecutionArgs arg)
	{
		var propertyName = arg.Method.Name.Replace("set_", string.Empty, StringComparison.Ordinal);
#pragma warning disable IL2075
		var property = arg.Instance.GetType().GetProperty(propertyName);
#pragma warning restore IL2075

		var oldValue = property?.GetValue(arg.Instance);
		var newValue = arg.Arguments.Length is 0 ? null : arg.Arguments[0];

		if (oldValue is IDisposable value && !Equals(oldValue, newValue))
			value.Dispose();
	}
}
