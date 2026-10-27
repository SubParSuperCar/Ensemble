namespace EnsembleRoot.Common.Utils;

/// <summary>A flag that stays set while any owner holds it, so owners can't release each other's holds.</summary>
/// <remarks>Thread-safe. Owners are compared by reference.</remarks>
public sealed class OwnershipFlag
{
	private readonly Lock _lock = new();
	private readonly HashSet<object> _owners = new(ReferenceEqualityComparer.Instance);

	public bool IsSet
	{
		get
		{
			lock (_lock)
				return _owners.Count > 0;
		}
	}

	public void Acquire(object owner)
	{
		lock (_lock)
			_owners.Add(owner);
	}

	public void Release(object owner)
	{
		lock (_lock)
			_owners.Remove(owner);
	}
}
