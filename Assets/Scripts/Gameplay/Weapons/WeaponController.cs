using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WeaponController : NetworkBehaviour
{
    [Header("Progression")]
    // FIX: Change 'WeaponData' to 'UpgradeData'
    public List<UpgradeData> allUpgradesPool;
    public float globalDamageMultiplier = 1.0f;

    private List<int> unlockedWeaponIndices = new List<int>();
    private List<BaseWeapon> activeWeapons = new List<BaseWeapon>();
    
    // Track all applied upgrade indices for persistence and UI display
    private List<int> appliedUpgradeHistory = new List<int>();

    [SerializeField] private WeaponData startingWeapon;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        Debug.Log($"[WeaponController] OnNetworkSpawn called! IsServer={IsServer}, IsOwner={IsOwner}, ClientId={OwnerClientId}, startingWeapon={(startingWeapon != null ? startingWeapon.weaponName : "NULL")}");
        
        if (IsServer && startingWeapon != null)
        {
            Debug.Log($"[WeaponController] Adding starting weapon: {startingWeapon.weaponName}");
            AddWeapon(startingWeapon);
        }
        else if (IsServer && startingWeapon == null)
        {
            Debug.LogWarning("[WeaponController] startingWeapon is NULL on server! Check prefab configuration.");
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        foreach (var weapon in activeWeapons)
        {
            weapon.WeaponUpdate();
        }
    }

    [ServerRpc]
    public void RequestUnlockWeaponServerRpc(int index)
    {
        // This now works because the list type matches the variable type
        if (index < 0 || index >= allUpgradesPool.Count) return;

        UpgradeData selectedUpgrade = allUpgradesPool[index];

        selectedUpgrade.Apply(gameObject);
        
        // Track this upgrade in history
        appliedUpgradeHistory.Add(index);

        Debug.Log($"[Upgrade] Player {OwnerClientId} picked {selectedUpgrade.upgradeName}");

        // After upgrade is applied, tell the client it's safe to resume
        ResumeGameplayClientRpc();
    }

    /// <summary>
    /// Directly applies an upgrade at the given index (server-side, no UI).
    /// Used for random upgrade selection on level up.
    /// </summary>
    public void ApplyUpgradeAtIndex(int index)
    {
        if (!IsServer) return;
        if (index < 0 || index >= allUpgradesPool.Count) return;

        UpgradeData selectedUpgrade = allUpgradesPool[index];
        selectedUpgrade.Apply(gameObject);
        
        // Track this upgrade in history
        appliedUpgradeHistory.Add(index);

        Debug.Log($"[Upgrade] Player {OwnerClientId} auto-selected: {selectedUpgrade.upgradeName}");
    }
    
    /// <summary>
    /// Get a copy of all applied upgrade indices. Used for saving upgrades on death.
    /// </summary>
    public List<int> GetAppliedUpgradeHistory()
    {
        return new List<int>(appliedUpgradeHistory);
    }
    
    /// <summary>
    /// Restore upgrades from a saved list (e.g., after respawn).
    /// </summary>
    public void RestoreUpgrades(List<int> upgradeIndices)
    {
        if (!IsServer) return;
        
        foreach (int index in upgradeIndices)
        {
            if (index >= 0 && index < allUpgradesPool.Count)
            {
                allUpgradesPool[index].Apply(gameObject);
                appliedUpgradeHistory.Add(index);
            }
        }
        
        Debug.Log($"[Upgrade] Restored {upgradeIndices.Count} upgrades for Player {OwnerClientId}");
    }
    
    /// <summary>
    /// Get a list of all applied upgrade names for UI display.
    /// </summary>
    public List<string> GetAppliedUpgradeNames()
    {
        List<string> names = new List<string>();
        foreach (int index in appliedUpgradeHistory)
        {
            if (index >= 0 && index < allUpgradesPool.Count)
            {
                names.Add(allUpgradesPool[index].upgradeName);
            }
        }
        return names;
    }

    [ClientRpc]
    private void ResumeGameplayClientRpc()
    {
        if (!IsOwner) return;

        // Re-enable movement/actions here if you disabled them
        // PlayerMovement.Instance.SetEnabled(true);
    }

    public void IncreaseGlobalDamage(float amount)
    {
        globalDamageMultiplier += amount;
    }

    public void AddWeapon(WeaponData newData)
    {
        if (newData == null) return;

        GameObject weaponObj = new GameObject($"Weapon_{newData.weaponName}");
        weaponObj.transform.SetParent(transform);
        weaponObj.transform.localPosition = Vector3.zero;

        System.Type type = System.Type.GetType(newData.behaviorScript);

        if (type != null)
        {
            BaseWeapon newWeapon = (BaseWeapon)weaponObj.AddComponent(type);
            newWeapon.Initialize(newData, NetworkObjectId);
            activeWeapons.Add(newWeapon);
        }
    }
}
