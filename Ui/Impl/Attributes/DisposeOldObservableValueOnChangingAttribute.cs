using MethodBoundaryAspect.Fody.Attributes;

namespace Root.Ui.Impl.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
[Serializable]
public class DisposeOldObservableValueOnChangingAttribute : OnMethodBoundaryAspect
{
	// Helper attribute to make it so when we overwrite observable values with View Models,
	// the old value is automatically disposed. Extremely helpful, but probably not fully NativeAOT/trimming safe.
	public override void OnEntry(MethodExecutionArgs arg)
	{
		var propertyName = arg.Method.Name.Replace("set_", "", StringComparison.Ordinal);
#pragma warning disable IL2075
		var property = arg.Instance.GetType().GetProperty(propertyName); // Works fine so far w/ NativeAOT & trimming.
#pragma warning restore IL2075

		var oldValue = property?.GetValue(arg.Instance);
		var newValue = arg.Arguments.Length > 0 ? arg.Arguments[0] : null;

		if (oldValue is IDisposable value && !Equals(oldValue, newValue))
			value.Dispose();
	}
}
