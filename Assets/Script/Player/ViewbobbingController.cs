using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class ViewbobbingController : NetworkBehaviour
{
    [Header("References")]
    public Transform cameraHolder;  
    public PlayerMV player;

    [Header("Viewbobbing Settings")]
    public float tiltAmountBase = 1f; // 0.65f
    public float tiltAmountRun = 2.3f; 
    public float tiltAmount = 1f; // 0.65f
    public float tiltSpeed = 5f;
    public float bobFrequency = 10f;
    public float bobAmplitude = 0.08f;
    public float bobAmplitudeBase = 0.08f;
    public float bobAmplitudeRun = 0.13f;
    public float bobAmplitudeCrouch = 0.04f;

    private Vector3 initialLocalPos;
    private float bobTimer = 0f;

    public override void OnStartAuthority()
    {
        initialLocalPos = cameraHolder.localPosition;
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name.Equals("Game") && isOwned)
        {

            Vector3 targetTilt = Vector3.zero;

            Vector3 moveDir = player.verticalInput * player.orientation.forward + player.horizontalInput * player.orientation.right;

            if (moveDir.sqrMagnitude > 0.01f)
            {
                moveDir.Normalize();

                Vector3 localDir = cameraHolder.InverseTransformDirection(moveDir);

                targetTilt.x = localDir.z * tiltAmount;
                targetTilt.z = -localDir.x * tiltAmount;
            }

            // Bobbing Efekti 
            if (Mathf.Abs(player.horizontalInput) > 0.1f || Mathf.Abs(player.verticalInput) > 0.1f)
            {
                bobTimer += Time.deltaTime * bobFrequency;
                float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude;
                cameraHolder.localPosition = initialLocalPos + new Vector3(0, bobOffset, 0);
            }
            else
            {
                bobTimer = 0;
                cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, initialLocalPos, Time.deltaTime * tiltSpeed);
            }

            // Tilt Rotasyonu Uygula (local)
            Quaternion targetRot = Quaternion.Euler(targetTilt);
            cameraHolder.localRotation = Quaternion.Slerp(cameraHolder.localRotation, targetRot, Time.deltaTime * tiltSpeed);
        }
    }
}
