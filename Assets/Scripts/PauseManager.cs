using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseMenuUI; // Assegna il Canvas del menu pausa nell'Inspector
    private bool isPaused = false;

    void Start()
    {
        // Assicura che il menu sia nascosto e il gioco sia attivo all'inizio
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    void Update()
    {
        if (PlayerDeathScreen.IsVisible)
        {
            return;
        }

        if (isPaused && GameInput.IsCancelPressedThisFrame())
        {
            Resume();
            return;
        }

        if (GameInput.IsPausePressedThisFrame())
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false); // Nascondi il menu
        Time.timeScale = 1f; // Ripristina il tempo normale
        isPaused = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true); // Mostra il menu
        Time.timeScale = 0f; // Ferma il gioco
        isPaused = true;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f; // Ripristina il tempo prima di cambiare scena
        SceneManager.LoadScene(0); // Carica la scena del menu principale (index 0)
    }

    public void QuitGame()
    {
        Debug.Log("Quit!");
        Application.Quit();
    }
}
