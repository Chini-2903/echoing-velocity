using UnityEngine;

public class Spinner : MonoBehaviour
{
    [Tooltip("Speed of rotation on the X, Y, and Z axes")]
    public Vector3 rotationSpeed = new Vector3(0, 0, 150f);

    void Update()
    {
        // Rotate the object around its local axes
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}