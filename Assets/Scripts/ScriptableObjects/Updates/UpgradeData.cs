using UnityEngine;

public abstract class UpgradeData : ScriptableObject
{
    [Header("UI Info")]
    public string upgradeName;
    [TextArea] public string description;
    // public Sprite icon; // Uncomment when you have icons

    // Every upgrade must implement this method
    public abstract void Apply(GameObject player);
}
