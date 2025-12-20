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

        // Initialize Unity Services
        await UnityServices.InitializeAsync();

        // Sign in anonymously
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // Load saved name if exists
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
}
