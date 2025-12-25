using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpUI : MonoBehaviour
{
    public static LevelUpUI Instance;

    [Header("Main UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private CanvasGroup canvasGroup; // For fade animation
    [SerializeField] private Image backdrop; // Dark overlay behind cards

    [Header("Upgrade Cards")]
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI[] optionNameTexts;
    [SerializeField] private TextMeshProUGUI[] optionDescTexts;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float cardScaleStartSize = 0.8f;

    private WeaponController localPlayerController;
    private List<int> currentOptions = new List<int>();

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void ShowOptions(WeaponController controller)
    {
        localPlayerController = controller;
        currentOptions.Clear();

        // 1. Create shuffled list of upgrade indices
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < controller.allUpgradesPool.Count; i++)
        {
            availableIndices.Add(i);
        }

        // Shuffle (Fisher-Yates)
        for (int i = 0; i < availableIndices.Count; i++)
        {
            int temp = availableIndices[i];
            int randomIndex = Random.Range(i, availableIndices.Count);
            availableIndices[i] = availableIndices[randomIndex];
            availableIndices[randomIndex] = temp;
        }

        // 2. Assign buttons
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i >= availableIndices.Count)
            {
                optionButtons[i].gameObject.SetActive(false);
                continue;
            }

            optionButtons[i].gameObject.SetActive(true);

            int pickedIndex = availableIndices[i];
            UpgradeData data = controller.allUpgradesPool[pickedIndex];

            // Update name and description texts
            if (optionNameTexts != null && i < optionNameTexts.Length && optionNameTexts[i] != null)
            {
                optionNameTexts[i].text = data.upgradeName;
            }
            if (optionDescTexts != null && i < optionDescTexts.Length && optionDescTexts[i] != null)
            {
                optionDescTexts[i].text = data.description;
            }

            // Setup click listener
            int indexToSend = pickedIndex;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => SelectUpgrade(indexToSend));
        }

        panel.SetActive(true);
        SetPlayerInput(false);

        // Start fade-in animation
        StartCoroutine(FadeInAnimation());
    }

    private IEnumerator FadeInAnimation()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        // Animate cards scaling up
        foreach (var button in optionButtons)
        {
            if (button != null && button.gameObject.activeSelf)
            {
                StartCoroutine(ScaleUpCard(button.transform));
            }
        }
    }

    private IEnumerator ScaleUpCard(Transform card)
    {
        card.localScale = Vector3.one * cardScaleStartSize;
        float elapsed = 0f;
        float duration = fadeInDuration * 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Ease out bounce effect
            t = 1f - Mathf.Pow(1f - t, 3f);
            card.localScale = Vector3.Lerp(Vector3.one * cardScaleStartSize, Vector3.one, t);
            yield return null;
        }
        card.localScale = Vector3.one;
    }

    private void SelectUpgrade(int weaponIndex)
    {
        if (localPlayerController != null)
        {
            localPlayerController.RequestUnlockWeaponServerRpc(weaponIndex);
        }

        panel.SetActive(false);
        SetPlayerInput(true);
    }

    private void SetPlayerInput(bool enabled)
    {
        if (localPlayerController == null) return;
        
        if (localPlayerController.TryGetComponent(out PlayerMovement move)) 
            move.enabled = enabled;
        localPlayerController.enabled = enabled;
    }
}
