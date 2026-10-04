using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;

public class ServerBrowserUI : MonoBehaviour
{
    [Header("Tworzenie Pokoju")]
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField] private Button createLobbyButton;

    [Header("Lista Pokoi")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Transform lobbyContainer;
    [SerializeField] private GameObject lobbyItemPrefab;

    [Header("Komunikaty")]
    [SerializeField] private TextMeshProUGUI statusText;

    private async void Start()
    {
        createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
        refreshButton.onClick.AddListener(OnRefreshListClicked);
        
        if (statusText != null) statusText.text = "Inicjalizacja usług sieciowych...";
        
        while (UnityServices.State != ServicesInitializationState.Initialized || 
               AuthenticationService.Instance == null || 
               !AuthenticationService.Instance.IsSignedIn)
        {
            await Task.Delay(200); // Odczekaj 0.2 sekundy i sprawdź ponownie
        }

        if (statusText != null) statusText.text = "Gotowy do gry!";
        RefreshLobbyList();
    }

    private async void OnCreateLobbyClicked()
    {
        string lobbyName = lobbyNameInput.text.Trim();
        if (string.IsNullOrEmpty(lobbyName))
        {
            lobbyName = "Pokój Warchoła";
        }

        if (statusText != null) statusText.text = "Tworzenie pokoju w chmurze...";
        string joinCode = await LobbyManager.Instance.CreateLobby(lobbyName);

        if (!string.IsNullOrEmpty(joinCode))
        {
            if (statusText != null) statusText.text = "Pokój stworzony! Oczekiwanie na drugiego gracza...";
            gameObject.SetActive(false);
        }
        else
        {
            if (statusText != null) statusText.text = "Błąd tworzenia pokoju! Sprawdź połączenie.";
        }
    }

    private void OnRefreshListClicked()
    {
        RefreshLobbyList();
    }

    private async void RefreshLobbyList()
    {
        if (!AuthenticationService.Instance.IsSignedIn) return;

        if (statusText != null) statusText.text = "Szukanie dostępnych serwerów...";
        
        foreach (Transform child in lobbyContainer)
        {
            Destroy(child.gameObject);
        }

        List<Lobby> lobbies = await LobbyManager.Instance.QueryLobbies();

        if (lobbies.Count == 0)
        {
            if (statusText != null) statusText.text = "Brak dostępnych serwerów. Stwórz własny pokój!";
            return;
        }

        if (statusText != null) statusText.text = $"Znalezione serwery: {lobbies.Count}";

        foreach (Lobby lobby in lobbies)
        {
            GameObject item = Instantiate(lobbyItemPrefab, lobbyContainer);
            
            TextMeshProUGUI itemText = item.GetComponentInChildren<TextMeshProUGUI>();
            if (itemText != null)
            {
                itemText.text = $"{lobby.Name}  [{lobby.Players.Count}/{lobby.MaxPlayers}]";
            }

            Button joinButton = item.GetComponentInChildren<Button>();
            if (joinButton != null)
            {
                joinButton.onClick.AddListener(async () =>
                {
                    if (statusText != null) statusText.text = $"Łączenie z pokojem: {lobby.Name}...";
                    bool success = await LobbyManager.Instance.JoinLobbyById(lobby.Id);
                    
                    if (success)
                    {
                        gameObject.SetActive(false);
                    }
                    else
                    {
                        if (statusText != null) statusText.text = "Nie udało się dołączyć do pokoju!";
                    }
                });
            }
        }
    }
}