using UnityEngine;

public class LevelExit : MonoBehaviour
{
    public GameObject winTextUI;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (winTextUI != null) winTextUI.SetActive(true);

            Time.timeScale = 0.05f; // Epic slow motion
            other.GetComponent<VelocityPlayer>().enabled = false;

            // Find the Jam Manager and show the "Press R to Restart" text
            JamManager manager = FindFirstObjectByType<JamManager>();
            if (manager != null)
            {
                manager.ShowRestartPrompt();
            }
        }
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, 45f * Time.deltaTime);
    }
}