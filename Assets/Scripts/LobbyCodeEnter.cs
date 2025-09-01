using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCodeEnter : MonoBehaviour
{
    [SerializeField] private InputField LobbyCodeInputField;

    public void EnterLobbyWithLobbyCode()
    {
        string codeText = LobbyCodeInputField.text;

        if (string.IsNullOrWhiteSpace(codeText))
        {
            Debug.LogWarning("Lobby kodu girilmedi.");
            return;
        }

        if (ulong.TryParse(codeText, out ulong lobbyID))
        {
            CSteamID steamLobbyID = new CSteamID(lobbyID);
            SteamMatchmaking.JoinLobby(steamLobbyID);
            Debug.Log($"Lobby kodu ile bağlanılıyor: {lobbyID}");
        }
        else
        {
            Debug.LogWarning("Geçersiz lobby kodu girildi!");
        }
    }
}
