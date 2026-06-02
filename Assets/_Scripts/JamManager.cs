using UnityEngine;
using UnityEngine.SceneManagement;

public class JamManager : MonoBehaviour
{
    public GameObject startMenuUI;
    public GameObject restartPromptUI;

    private bool gameHasStarted = false;
    private bool gameOver = false;

    void Start()
    {
        // Freeze time and show the menu on boot
        Time.timeScale = 0f;
        startMenuUI.SetActive(true);
        restartPromptUI.SetActive(false);
    }

    void Update()
    {
        // Start the game
        if (!gameHasStarted && Input.GetKeyDown(KeyCode.Return)) // Return is the Enter key
        {
            gameHasStarted = true;
            startMenuUI.SetActive(false);

            // Unfreeze time
            Time.timeScale = 1f;
        }

        // Restart the game (works anytime after starting)
        if (gameHasStarted && Input.GetKeyDown(KeyCode.R))
        {
            // Unfreeze time just in case they are restarting from the slow-mo win screen
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // Quit the game (Good for standalone builds)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    // Call this from your LevelExit script
    public void ShowRestartPrompt()
    {
        gameOver = true;
        restartPromptUI.SetActive(true);
    }
}