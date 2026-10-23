using UnityEngine;

public class Rotator : MonoBehaviour
{
    public Vector3 degreesPerSecond = new Vector3(45f, 70f, 45f);

    private void Update()
    {
        transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
