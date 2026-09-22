namespace DoAction.Core;

// Existing held keys (e.g. Enter used to start) are ignored until released.
public sealed class PhysicalKeyGate(IEnumerable<int> initiallyHeld)
{
    private readonly HashSet<int> held = new(initiallyHeld);
    public bool InitialKeysReleased => held.Count == 0;
    public bool ShouldStop(int key, bool keyDown, bool injected)
    {
        if (injected) return false;
        if (!keyDown) { held.Remove(key); return false; }
        return !held.Contains(key);
    }
}
