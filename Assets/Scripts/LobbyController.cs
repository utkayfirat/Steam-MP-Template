using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Mirror.Examples.Basic;


public class LobbyController : MonoBehaviour
{   
    // UI Elements
    public static LobbyController Instance;
    public Text LobbyNameText, LobbyCodeText;

    // Player Data
    public GameObject PlayerListViewContent;
    public GameObject PlayerListItemPrefab;
    public GameObject LocalPlayerObject;

    // Other Data
    public ulong CurrentLobbyID;
    public bool PlayerItemCreated = false;
    private List<PlayerListItem> PlayerListItems = new List<PlayerListItem>();
    public PlayerObjectController LocalplayerController;

    // Ready
    public Button StartGateButton;
    public Text ReadyButtonText;

    // Manager
    private CustomNetworkManager manager;
    private CustomNetworkManager Manager
    {
        get
        {
            if (manager != null) return manager;
            return manager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ReadyPlayer()
    {
        LocalplayerController.ChangeReady();
    }

    public void UpdateButton()
    {
        if (LocalplayerController.Ready)
        {
            ReadyButtonText.text = "Not Ready";
        }
        else
        {
            ReadyButtonText.text = "Ready Up";
        }
    }

    public void CheckIfAllReady()
    {
        bool AllReady = false;
        foreach (PlayerObjectController player in Manager.GamePlayers)
        {
            if (player.Ready)
            {
                AllReady = true;
            }
            else
            {
                AllReady = false;
                break;
            }
        }

        if (AllReady)
        {
            if (LocalplayerController.PlayerIdNumber == 1)
            {
                StartGateButton.interactable = true;
            }
            else
            {
                StartGateButton.interactable = false;
            }
        }
        else
        {
            StartGateButton.interactable = false;
        }
    }

    public void UpdateLobbyName()
    {
        CurrentLobbyID = Manager.GetComponent<SteamLobby>().CurrentLobbyID;
        LobbyNameText.text = SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyID), "name");
        LobbyCodeText.text = SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyID), "code");

    }

    public void UpdatePlayerList()
    {
        if (!PlayerItemCreated) CreateHostPlayerItem();
        if (PlayerListItems.Count < Manager.GamePlayers.Count) CreateClientPlayerItem();
        if (PlayerListItems.Count > Manager.GamePlayers.Count) RemovePlayerItem();
        if (PlayerListItems.Count == Manager.GamePlayers.Count) UpdatePlayerItem();
    }

    public void FindLocalPlayer()
    {
        LocalPlayerObject = GameObject.Find("LocalGamePlayer");
        LocalplayerController = LocalPlayerObject.GetComponent<PlayerObjectController>();
    }

    public void CreateHostPlayerItem()
    {
        foreach (PlayerObjectController player in Manager.GamePlayers)
        {
            GameObject NewPlayerItem = Instantiate(PlayerListItemPrefab) as GameObject;
            PlayerListItem NewPlayerItemScript = NewPlayerItem.GetComponent<PlayerListItem>();

            NewPlayerItemScript.PlayerName = player.PlayerName;
            NewPlayerItemScript.ConnectionID = player.ConnectionID;
            NewPlayerItemScript.PlayerSteamID = player.PlayerSteamID;
            NewPlayerItemScript.Ready = player.Ready;
            NewPlayerItemScript.SetPlayerValues();

            NewPlayerItem.transform.SetParent(PlayerListViewContent.transform);
            NewPlayerItem.transform.localScale = Vector3.one;

            PlayerListItems.Add(NewPlayerItemScript);
        }
        PlayerItemCreated = true;
    }

    public void CreateClientPlayerItem()
    {
        foreach (PlayerObjectController player in Manager.GamePlayers)
        {
            if (!PlayerListItems.Any(b => b.ConnectionID == player.ConnectionID))
            {
                GameObject NewPlayerItem = Instantiate(PlayerListItemPrefab) as GameObject;
                PlayerListItem NewPlayerItemScript = NewPlayerItem.GetComponent<PlayerListItem>();

                NewPlayerItemScript.PlayerName = player.PlayerName;
                NewPlayerItemScript.ConnectionID = player.ConnectionID;
                NewPlayerItemScript.PlayerSteamID = player.PlayerSteamID;
                NewPlayerItemScript.Ready = player.Ready;
                NewPlayerItemScript.SetPlayerValues();

                NewPlayerItem.transform.SetParent(PlayerListViewContent.transform);
                NewPlayerItem.transform.localScale = Vector3.one;

                PlayerListItems.Add(NewPlayerItemScript);
            }
        }
    }

    public void UpdatePlayerItem()
    {
        foreach (PlayerObjectController player in Manager.GamePlayers)
        {
            foreach (PlayerListItem PlayerListItemScript in PlayerListItems)
            {
                if (PlayerListItemScript.ConnectionID == player.ConnectionID)
                {
                    PlayerListItemScript.PlayerName = player.PlayerName;
                    PlayerListItemScript.Ready = player.Ready;
                    PlayerListItemScript.SetPlayerValues();
                    if (player == LocalplayerController)
                    {
                        UpdateButton();
                    }
                }
            }
        }
        CheckIfAllReady();
    }

    public void RemovePlayerItem()
    {
        List<PlayerListItem> playerListItemToRemove = new List<PlayerListItem>();

        foreach (PlayerListItem playerListItem in PlayerListItems)
        {
            if (!Manager.GamePlayers.Any(b => b.ConnectionID == playerListItem.ConnectionID))
            {
                playerListItemToRemove.Add(playerListItem);
            }
        }
        if (playerListItemToRemove.Count > 0)
        {
            foreach (PlayerListItem playerlistItemToRemove in playerListItemToRemove)
            {
                GameObject ObjectToRemove = playerlistItemToRemove.gameObject;
                PlayerListItems.Remove(playerlistItemToRemove);
                Destroy(ObjectToRemove);
                ObjectToRemove = null;
            }
        }
    }

    public void StartGame(string SceneName)
    {
        LocalplayerController.CanStartGame(SceneName);
    }


    #region Lobby Code (Custom)
    public void CopyLobbyCodeToClipboard()
    {
        GUIUtility.systemCopyBuffer = LobbyCodeText.text;
        Debug.Log("Lobby kodu panoya kopyalandı: " + LobbyCodeText.text);
    }
    #endregion

    #region Leave Lobby (Custom) 
    public void ExitLobby()
    {
        if (NetworkServer.active || NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else
        {
            NetworkManager.singleton.StopClient();
        }

        if (CurrentLobbyID != 0)
        {
            SteamMatchmaking.LeaveLobby(new CSteamID(CurrentLobbyID));
            Debug.Log("Lobby'den çıkıldı.");
            CurrentLobbyID = 0;
        }
    }
    #endregion
}
