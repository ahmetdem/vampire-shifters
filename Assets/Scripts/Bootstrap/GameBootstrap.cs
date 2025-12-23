using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameBootstrap : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button connectButton;

    private async void Start()
    {
        connectButton.interactable = false;

        // 1. Setup Initialization Options for unique player profiles
        InitializationOptions options = new InitializationOptions();

#if UNITY_EDITOR
        // If we are a ParrelSync clone, use its argument as profile
        if (ParrelSync.ClonesManager.IsClone())
        {
            string cloneName = ParrelSync.ClonesManager.GetArgument();
            options.SetProfile(cloneName);
        }
        else
        {
            // For different PCs/editors using the same project, generate a unique profile
            // This ensures each machine gets its own player ID
            string uniqueEditorId = PlayerPrefs.GetString("UniqueEditorProfileId", "");
            if (string.IsNullOrEmpty(uniqueEditorId))
            {
                // Generate a new unique ID for this editor instance
                // Sanitize device name: only keep alphanumeric and underscore, max 20 chars
                string sanitizedDeviceName = SanitizeProfileName(SystemInfo.deviceName, 20);
                string guidPart = System.Guid.NewGuid().ToString("N").Substring(0, 8); // "N" = no hyphens
                uniqueEditorId = $"{sanitizedDeviceName}_{guidPart}";
                PlayerPrefs.SetString("UniqueEditorProfileId", uniqueEditorId);
                PlayerPrefs.Save();
                Debug.Log($"[Auth] Generated new editor profile: {uniqueEditorId}");
            }
            options.SetProfile(uniqueEditorId);
            Debug.Log($"[Auth] Using editor profile: {uniqueEditorId}");
        }
#endif

        // 2. Initialize with those options
        await UnityServices.InitializeAsync(options);

        // 3. Sign in (This will now result in a unique PlayerID per profile)
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // Load saved name
        string savedName = PlayerPrefs.GetString("PlayerName", "");
        nameInput.text = savedName;

        nameInput.onValueChanged.AddListener(ValidateName);
        connectButton.onClick.AddListener(EnterLobby);

        ValidateName(savedName);
    }
    private void ValidateName(string name)
    {
        // Simple validation: 2-12 characters
        bool isValid = name.Length >= 2 && name.Length <= 12;
        connectButton.interactable = isValid;
    }

    private void EnterLobby()
    {
        // Save name for the next scene
        PlayerPrefs.SetString("PlayerName", nameInput.text);

        // Load the Main Menu
        SceneManager.LoadScene("01_MainMenu");
    }

    /// <summary>
    /// Sanitizes a string to only contain valid profile name characters (a-z, A-Z, 0-9, _)
    /// </summary>
    private static string SanitizeProfileName(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input)) return "Player";

        var sb = new System.Text.StringBuilder();
        foreach (char c in input)
        {
            // Only keep alphanumeric and underscore
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sb.Append(c);
            }
        }

        string result = sb.ToString();
        
        // Ensure we have at least something
        if (string.IsNullOrEmpty(result)) result = "Player";
        
        // Limit length
        if (result.Length > maxLength) result = result.Substring(0, maxLength);
        
        return result;
    }
}
