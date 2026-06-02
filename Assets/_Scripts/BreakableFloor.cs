using UnityEngine;

public class BreakableFloor : MonoBehaviour
{
    [Header("Smash Settings")]
    public float speedRetention = 0.6f;
    public float requiredFallSpeed = -20f;

    [Header("Audio")]
    public AudioClip glassSmashClip; // NOTE: This is an AudioClip, NOT an AudioSource!

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            VelocityPlayer player = other.GetComponent<VelocityPlayer>();

            if (player != null)
            {
                if (Physics.gravity.y < -40f && player.velocity.y <= requiredFallSpeed)
                {
                    player.ApplySmashResistance(speedRetention);

                    if (Camera.main != null)
                    {
                        CameraShake shaker = Camera.main.GetComponent<CameraShake>();
                        if (shaker != null) StartCoroutine(shaker.Shake(0.15f, 0.4f));
                    }

                    // Play the sound independently in 3D space so it survives the floor's destruction
                    if (glassSmashClip != null)
                    {
                        AudioSource.PlayClipAtPoint(glassSmashClip, transform.position, 1.0f);
                    }

                    Destroy(gameObject);
                }
            }
        }
    }
}