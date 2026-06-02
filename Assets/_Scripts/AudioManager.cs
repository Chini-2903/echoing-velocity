using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource laserDeathSource;

    private bool isDying = false;

    void Awake()
    {
        // Singleton pattern ensures easy access
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void TriggerLaserDeath()
    {
        if (isDying) return; // Prevent double-triggering
        isDying = true;

        if (laserDeathSource != null) laserDeathSource.Play();
        StartCoroutine(FadeBGMAndRestart(1.2f)); // 1.2 second fade duration
    }

    private IEnumerator FadeBGMAndRestart(float duration)
    {
        float startVol = bgmSource != null ? bgmSource.volume : 0;
        float time = 0;

        // Freeze the player so they don't keep falling while the screen fades
        VelocityPlayer player = FindFirstObjectByType<VelocityPlayer>();
        if (player != null) player.enabled = false;

        // Fade out the BGM smoothly
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            if (bgmSource != null)
            {
                bgmSource.volume = Mathf.Lerp(startVol, 0f, time / duration);
            }
            yield return null;
        }

        // Reload the timeline (BGM will restart on load)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}