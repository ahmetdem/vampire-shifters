using UnityEngine;

[CreateAssetMenu(menuName = "Shift/Upgrades/Weapon Upgrade")]
public class WeaponUpgrade : UpgradeData
{
    public WeaponData weaponData;

    public override void Apply(GameObject player)
    {
        if (player.TryGetComponent(out WeaponController controller))
        {
            // This logic allows for duplicates (2 Wands) or new weapons
            controller.AddWeapon(weaponData);
        }
    }
}
