using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class SaveData
{
    public string sceneName;
    public float positionX;
    public float positionY;
    public float positionZ;
    public float currentHealth;
    public float maxHealth;
    public int level;
    public int currentXP;
    public int xpToNextLevel;
    public List<string> defeatedBossIds;
    public List<string> collectedFlowerIds;
    public List<string> completedArenaIds;
    public List<string> defeatedEnemyIds;
    public string savedAtUtc;
}

public static class SaveSystem
{
    private const string SaveFileName = "savegame.json";

    private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool SavePlayer(GameObject player)
    {
        if (player == null)
        {
            Debug.LogWarning("⚠️ Save fallito: player nullo.");
            return false;
        }

        return SavePlayer(player, player.transform.position);
    }

    public static bool SavePlayer(GameObject player, Vector3 savePosition)
    {
        if (player == null)
        {
            Debug.LogWarning("⚠️ Save fallito: player nullo.");
            return false;
        }

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        PlayerExperience experience = player.GetComponent<PlayerExperience>();

        if (health == null || experience == null)
        {
            Debug.LogWarning("⚠️ Save fallito: PlayerHealth o PlayerExperience mancanti.");
            return false;
        }

        SaveData data = new SaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            positionX = savePosition.x,
            positionY = savePosition.y,
            positionZ = savePosition.z,
            currentHealth = health.GetCurrentHealth(),
            maxHealth = health.GetMaxHealth(),
            level = experience.currentLevel,
            currentXP = experience.currentXP,
            xpToNextLevel = experience.xpToNextLevel,
            defeatedBossIds = new List<string>(GameProgress.Instance.DefeatedBossIds),
            collectedFlowerIds = new List<string>(GameProgress.Instance.CollectedFlowerIds),
            completedArenaIds = new List<string>(GameProgress.Instance.CompletedArenaIds),
            defeatedEnemyIds = new List<string>(GameProgress.Instance.DefeatedEnemyIds),
            savedAtUtc = DateTime.UtcNow.ToString("o")
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SaveFilePath, json);
        Debug.Log($"💾 Salvataggio completato: {SaveFilePath} | Pos=({savePosition.x:F2},{savePosition.y:F2},{savePosition.z:F2}) | Scene={data.sceneName}");
        return true;
    }

    public static bool TryLoad(out SaveData data)
    {
        if (!File.Exists(SaveFilePath))
        {
            data = null;
            return false;
        }

        string json = File.ReadAllText(SaveFilePath);
        data = JsonUtility.FromJson<SaveData>(json);
        return data != null;
    }

    public static bool TryLoadForCurrentScene(out SaveData data)
    {
        if (!TryLoad(out data))
        {
            Debug.Log("💾 Nessun save trovato.");
            return false;
        }

        string currentScene = NormalizeSceneName(SceneManager.GetActiveScene().name);
        string savedScene = NormalizeSceneName(data.sceneName);

        bool sameScene = string.Equals(savedScene, currentScene, StringComparison.OrdinalIgnoreCase);
        if (!sameScene)
        {
            Debug.Log($"💾 Save trovato ma scena diversa. Save='{data.sceneName}' Current='{SceneManager.GetActiveScene().name}'");
        }
        else
        {
            Debug.Log($"💾 Save valido per scena corrente: {data.sceneName}");
        }

        return sameScene;
    }

    public static void DeleteSave()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
            Debug.Log("🗑️ Save eliminato.");
        }
    }

    private static string NormalizeSceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return string.Empty;
        }

        string name = sceneName.Trim();
        name = name.Replace('\\', '/');
        name = Path.GetFileNameWithoutExtension(name);
        return name;
    }
}
