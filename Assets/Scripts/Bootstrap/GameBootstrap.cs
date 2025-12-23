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
        string profileToUse = "default";

#if UNITY_EDITOR
        // If we are a ParrelSync clone, use its argument as profile
        if (ParrelSync.ClonesManager.IsClone())
        {
            string cloneName = ParrelSync.ClonesManager.GetArgument();
            profileToUse = cloneName;
            Debug.Log($"[Auth] ParrelSync clone detected, using profile: {profileToUse}");
        }
        else
        {
            // For different PCs/editors using the same project, generate a unique profile
            profileToUse = GetOrCreateUniqueEditorProfile();
        }
        
        options.SetProfile(profileToUse);
        Debug.Log($"[Auth] Attempting to initialize with profile: '{profileToUse}' (length: {profileToUse.Length})");
#endif

        // 2. Initialize with those options (with error recovery)
        try
        {
            await UnityServices.InitializeAsync(options);
            Debug.Log("[Auth] Unity Services initialized successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Auth] Initialization failed with profile '{profileToUse}': {e.Message}");
            
#if UNITY_EDITOR
            // Clear the bad profile and try with a simple fallback
            PlayerPrefs.DeleteKey("UniqueEditorProfileId");
            PlayerPrefs.Save();
            
            string fallbackProfile = $"Player{Random.Range(10000, 99999)}";
            Debug.Log($"[Auth] Retrying with fallback profile: {fallbackProfile}");
            
            options = new InitializationOptions();
            options.SetProfile(fallbackProfile);
            
            try
            {
                await UnityServices.InitializeAsync(options);
                // Save the working profile for next time
                PlayerPrefs.SetString("UniqueEditorProfileId", fallbackProfile);
                PlayerPrefs.Save();
                Debug.Log($"[Auth] Fallback succeeded! Saved profile: {fallbackProfile}");
            }
            catch (System.Exception e2)
            {
                Debug.LogError($"[Auth] Fallback also failed: {e2.Message}");
                Debug.LogError("[Auth] Full exception: " + e2.ToString());
                return; // Cannot continue
            }
#else
            return; // Cannot continue in build
#endif
        }

        // 3. Sign in (This will now result in a unique PlayerID per profile)
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[Auth] Signed in with PlayerID: {AuthenticationService.Instance.PlayerId}");
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

#if UNITY_EDITOR
    /// <summary>
    /// Gets or creates a unique profile ID for this editor instance.
    /// </summary>
    private string GetOrCreateUniqueEditorProfile()
    {
        string uniqueEditorId = PlayerPrefs.GetString("UniqueEditorProfileId", "");
        
        if (string.IsNullOrEmpty(uniqueEditorId))
        {
            // Generate a simple, safe profile name
            // Only use alphanumeric - avoid any special chars completely
            string sanitizedDeviceName = SanitizeProfileName(SystemInfo.deviceName, 15);
            string guidPart = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            uniqueEditorId = $"{sanitizedDeviceName}{guidPart}";
            
            // Extra safety: ensure total length is reasonable (max 30)
            if (uniqueEditorId.Length > 30)
            {
                uniqueEditorId = uniqueEditorId.Substring(0, 30);
            }
            
            PlayerPrefs.SetString("UniqueEditorProfileId", uniqueEditorId);
            PlayerPrefs.Save();
            Debug.Log($"[Auth] Generated new editor profile: {uniqueEditorId}");
        }
        
        return uniqueEditorId;
    }
#endif
}
