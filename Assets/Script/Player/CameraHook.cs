using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class CameraHook : NetworkBehaviour
{
    public Transform cameraPosition;
    [SerializeField] private bool camNom = false;

    void Update()
    {
        if (!SceneManager.GetActiveScene().name.Equals("Game")) return; // && !isOwned
        
        if(camNom)
            transform.position = cameraPosition.position+new Vector3(0,0.16f,0);
        else
            transform.position = cameraPosition.position;

        transform.rotation = cameraPosition.rotation;
    }
}
