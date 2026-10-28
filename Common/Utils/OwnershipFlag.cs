// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace EnsembleRoot.Common.Utils;

/// <summary>A flag that stays set while any owner holds it, so owners can't release each other's holds.</summary>
/// <remarks>Thread-safe. Owners are compared by reference.</remarks>
public sealed class OwnershipFlag
{
	private readonly Lock _lock = new();
	private readonly HashSet<object> _owners = new(ReferenceEqualityComparer.Instance);

	public bool IsSet => Count > 0;

	public int Count
	{
		get
		{
			lock (_lock)
				return _owners.Count;
		}
	}

	public bool IsHeldBy(object owner)
	{
		lock (_lock)
			return _owners.Contains(owner);
	}

	public bool Acquire(object owner)
	{
		lock (_lock)
			return _owners.Add(owner);
	}

	public bool Release(object owner)
	{
		lock (_lock)
			return _owners.Remove(owner);
	}

	public void ReleaseAll()
	{
		lock (_lock)
			_owners.Clear();
	}
}
