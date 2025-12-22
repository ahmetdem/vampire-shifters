using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpUI : MonoBehaviour
{
    public static LevelUpUI Instance; // Singleton so Player can find it easily

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI[] optionTexts;
    // Optional: Add Image[] optionIcons if you want to show sprites

    private WeaponController localPlayerController;
    private List<int> currentOptions = new List<int>();

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false); // Hide on start
    }

    public void ShowOptions(WeaponController controller)
    {
        localPlayerController = controller;
        currentOptions.Clear();

        // 1. Create a list of all valid indices [0, 1, 2, 3...]
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < controller.allUpgradesPool.Count; i++)
        {
            // Optional: You can add an "if" here to skip maxed-out weapons later
            availableIndices.Add(i);
        }

        // 2. Shuffle the list (Fisher-Yates Shuffle)
        // This ensures we pick unique items without duplicates
        for (int i = 0; i < availableIndices.Count; i++)
        {
            int temp = availableIndices[i];
            int randomIndex = Random.Range(i, availableIndices.Count);
            availableIndices[i] = availableIndices[randomIndex];
            availableIndices[randomIndex] = temp;
        }

        // 3. Assign Buttons
        for (int i = 0; i < optionButtons.Length; i++)
        {
            // If we ran out of upgrades (e.g. only have 2 items but 3 buttons), hide the button
            if (i >= availableIndices.Count)
            {
                optionButtons[i].gameObject.SetActive(false);
                continue;
            }

            // Otherwise, show the button
            optionButtons[i].gameObject.SetActive(true);

            int pickedIndex = availableIndices[i]; // Pick from the shuffled list
            UpgradeData data = controller.allUpgradesPool[pickedIndex];

            // Update Text
            optionTexts[i].text = $"{data.upgradeName}\n<size=60%>{data.description}</size>";

            // Setup Click Listener
            int indexToSend = pickedIndex;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => SelectUpgrade(indexToSend));
        }

        panel.SetActive(true);
        SetPlayerInput(false);
    }

    private void SelectUpgrade(int weaponIndex)
    {
        // 4. Send request to Server
        if (localPlayerController != null)
        {
            localPlayerController.RequestUnlockWeaponServerRpc(weaponIndex);
        }

        // 5. Hide Panel
        panel.SetActive(false);
        SetPlayerInput(true);
    }

    private void SetPlayerInput(bool enabled)
    {
        // This looks for your local player's movement and stops it
        if (localPlayerController.TryGetComponent(out PlayerMovement move)) move.enabled = enabled;
        localPlayerController.enabled = enabled; // Stops the WeaponController from firing
    }
}
