using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class MouseLook : NetworkBehaviour
{

    public float sensX, sensY;

    public Transform orientation;

    private float yRotation, xRotation;

    void Update()
    {
        if (SceneManager.GetActiveScene().name.Equals("Game") && isOwned)
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
            float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

            yRotation += mouseX;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            
            Quaternion mouseRot = Quaternion.Euler(xRotation, yRotation, 0f);
            transform.localRotation = mouseRot;

            orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }
    }



}
