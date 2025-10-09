using UnityEngine;
using TMPro;

public class ProgressHUD : MonoBehaviour
{
    [SerializeField] TMP_Text text;

    void Reset() { text = GetComponent<TMP_Text>(); }

    public void Set(int current, int total)
    {
        if (!text) return;
        text.text = $"{current}/{total}";
    }

    public void SetLabelled(string label, int current, int total)
    {
        if (!text) return;
        text.text = $"{label}  {current}/{total}";
    }
}
