using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;
    public float smoothTime = 0.18f;

    private Vector3 offset;
    private Vector3 velocity;

    private void Start()
    {
        if (player != null)
            offset = transform.position - player.transform.position;
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        Vector3 targetPosition = player.transform.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}
