using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalLocalPos;

    void Start()
    {
        // Store the default camera position relative to the player
        originalLocalPos = transform.localPosition;
    }

    public IEnumerator Shake(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // Generate random movement
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // Apply it
            transform.localPosition = new Vector3(originalLocalPos.x + x, originalLocalPos.y + y, originalLocalPos.z);

            elapsed += Time.deltaTime;
            yield return null; // Wait for next frame
        }

        // Reset precisely back to normal
        transform.localPosition = originalLocalPos;
    }
}