using Mirror;
using UnityEngine;

public class PlayerVoiceIntegration : NetworkBehaviour
{
    public override void OnStartLocalPlayer()
    {
        gameObject.AddComponent<SteamVoiceSender>();
    }

    public override void OnStartClient()
    {
        if (!isLocalPlayer)
        {
            gameObject.AddComponent<SteamVoiceReceiver>();
        }
    }
}
