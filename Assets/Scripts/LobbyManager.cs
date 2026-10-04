using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer;
    private float lobbyUpdateTimer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
    }
    
    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }
    
    public async Task<string> CreateLobby(string lobbyName, bool isPrivate = false)
    {
        try
        {
            string relayCode = await RelayManager.Instance.CreateRelay();

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "JOIN_CODE", new DataObject(
                            DataObject.VisibilityOptions.Member, 
                            relayCode
                        )
                    }
                }
            };
            
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 2, options);
            hostLobby = lobby;
            joinedLobby = hostLobby;

            Debug.Log($"Stworzono Lobby: {lobby.Name} | ID: {lobby.Id}");
            return relayCode;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Błąd tworzenia Lobby: {e}");
            return null;
        }
    }
    
    public async Task<List<Lobby>> QueryLobbies()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 10,
                Filters = new List<QueryFilter>
                {
                    // Pokazuj tylko pokoje, w których jest wolne miejsce
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };

            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(options);
            return queryResponse.Results;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Błąd pobierania listy pokoi: {e}");
            return new List<Lobby>();
        }
    }
    
    public async Task<bool> JoinLobbyById(string lobbyId)
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            joinedLobby = lobby;
            
            string relayCode = joinedLobby.Data["JOIN_CODE"].Value;
            
            return await RelayManager.Instance.JoinRelay(relayCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Błąd dołączania do Lobby: {e}");
            return false;
        }
    }
}