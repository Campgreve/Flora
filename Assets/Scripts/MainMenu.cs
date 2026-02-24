using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private void Update()
    {
        if (GameInput.IsConfirmPressedThisFrame())
        {
            PlayGame();
        }
        else if (GameInput.IsCancelPressedThisFrame())
        {
            QuitGame();
        }
    }

    public void PlayGame()
    {
        Time.timeScale = 1f; // Assicura che il tempo sia normale

        // Se esiste un salvataggio valido, continua da quella scena.
        if (SaveSystem.TryLoad(out SaveData data) && data != null && !string.IsNullOrWhiteSpace(data.sceneName))
        {
            // Precarica il progresso globale prima del cambio scena
            GameProgress.Instance.LoadProgress(
                data.defeatedBossIds,
                data.collectedFlowerIds,
                data.completedArenaIds,
                data.defeatedEnemyIds);

            string targetScene = NormalizeSceneName(data.sceneName);
            Debug.Log($"▶️ Continue dal save: scena '{targetScene}'");
            SceneManager.LoadScene(targetScene);
            return;
        }

        // Fallback: nuova partita dal primo livello dopo il menu.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Debug.Log("Quit!");
        Application.Quit();
    }

    private static string NormalizeSceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return string.Empty;
        }

        string normalized = sceneName.Trim().Replace('\\', '/');
        int slashIndex = normalized.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex < normalized.Length - 1)
        {
            normalized = normalized.Substring(slashIndex + 1);
        }

        int dotIndex = normalized.LastIndexOf('.');
        if (dotIndex > 0)
        {
            normalized = normalized.Substring(0, dotIndex);
        }

        return normalized;
    }
}
