using UnityEngine;

[DisallowMultipleComponent]
public class CountableTarget : MonoBehaviour
{
    public bool counted { get; private set; }
    public System.Action<CountableTarget> onCounted;

    public void MarkCounted()
    {
        if (counted) return;
        counted = true;
        onCounted?.Invoke(this);
    }
}