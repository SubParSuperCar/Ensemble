// ReSharper disable UnusedMember.Global

namespace Root.Globals.Input;

public sealed class OwnershipFlag
{
	private readonly HashSet<object> _owners = [];

	public bool IsSet => _owners.Count is not 0;
	public int Count => _owners.Count;

	public bool IsHeldBy(object owner) => _owners.Contains(owner);

	public void Acquire(object owner) => _owners.Add(owner);
	public void Release(object owner) => _owners.Remove(owner);

	public void ReleaseAll() => _owners.Clear();
}
