using UnityEngine;

public enum StatType
{
    MaxHealth,
    MoveSpeed,
    DamageMultiplier,
    Heal
}

[CreateAssetMenu(menuName = "Shift/Upgrades/Stat Upgrade")]
public class StatUpgrade : UpgradeData
{
    public StatType statType;
    public float value; // e.g., 10 for HP, 0.1 for 10% speed

    public override void Apply(GameObject player)
    {
        // Handle Health/Healing
        if (statType == StatType.MaxHealth || statType == StatType.Heal)
        {
            if (player.TryGetComponent(out Health health))
            {
                if (statType == StatType.MaxHealth) health.IncreaseMaxHealth((int)value);
                if (statType == StatType.Heal) health.Heal((int)value);
            }
        }

        // Handle Movement
        if (statType == StatType.MoveSpeed)
        {
            if (player.TryGetComponent(out PlayerMovement movement))
            {
                // You'll need to add a method to your Movement script to change speed
                movement.ModifySpeed(value);
            }
        }

        // Handle Global Damage (We need to add this to WeaponController)
        if (statType == StatType.DamageMultiplier)
        {
            if (player.TryGetComponent(out WeaponController controller))
            {
                controller.IncreaseGlobalDamage(value);
            }
        }
    }
}
