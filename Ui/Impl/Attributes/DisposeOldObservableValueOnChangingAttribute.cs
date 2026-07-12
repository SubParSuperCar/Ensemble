using MethodBoundaryAspect.Fody.Attributes;

namespace Root.Ui.Impl.Attributes;

// Automatically disposes old values that have been overwritten in observable properties (to reduce a lot of boilerplate)
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
[Serializable]
public class DisposeOldObservableValueOnChangingAttribute : OnMethodBoundaryAspect
{
	public override void OnEntry(MethodExecutionArgs arg)
	{
		var propName = arg.Method.Name.Replace("set_", "", StringComparison.Ordinal);
		var prop = arg.Instance.GetType().GetProperty(propName);

		var oldValue = prop?.GetValue(arg.Instance);
		var newValue = arg.Arguments.Length > 0 ? arg.Arguments[0] : null;

		if (oldValue is IDisposable value && !Equals(oldValue, newValue))
			value.Dispose();
	}
}
