using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class PlayerMovementController : NetworkBehaviour
{
    public float Speed = 0.1f;
    public GameObject PlayerModel;

    void Start()
    {
        PlayerModel.SetActive(false);
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name == "Game")
        {
            if (!PlayerModel.activeSelf)
            {
                SetPosition();
                PlayerModel.SetActive(true);
            }

            if (isOwned)
            {
                Movement();
            }
        }
    }

    public void SetPosition()
    {
        transform.position = new Vector3(Random.Range(-5, 5), 0.8f, Random.Range(-15,7));
    }

    public void Movement()
    {
        float xDirection = Input.GetAxis("Horizontal");
        float zDirection = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(xDirection, 0, zDirection);

        transform.position += moveDirection * Speed;
    }
}
