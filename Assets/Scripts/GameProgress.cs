using System.Collections.Generic;
using UnityEngine;

public class GameProgress : MonoBehaviour
{
    private static GameProgress instance;
    public static GameProgress Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameProgress>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameProgress");
                    instance = go.AddComponent<GameProgress>();
                }
            }

            return instance;
        }
    }

    [SerializeField] private List<string> defeatedBossIds = new List<string>();
    [SerializeField] private List<string> collectedFlowerIds = new List<string>();
    [SerializeField] private List<string> completedArenaIds = new List<string>();
    [SerializeField] private List<string> defeatedEnemyIds = new List<string>();
    private bool loadedFromSave = false;

    public IReadOnlyList<string> DefeatedBossIds => defeatedBossIds;
    public IReadOnlyList<string> CollectedFlowerIds => collectedFlowerIds;
    public int DefeatedBossCount => defeatedBossIds.Count;
    public int FlowerCount => collectedFlowerIds.Count;
    public IReadOnlyList<string> CompletedArenaIds => completedArenaIds;
    public IReadOnlyList<string> DefeatedEnemyIds => defeatedEnemyIds;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        TryAutoLoadFromSave();
    }

    public bool HasDefeatedBoss(string bossId)
    {
        return !string.IsNullOrWhiteSpace(bossId) && defeatedBossIds.Contains(bossId);
    }

    public bool MarkBossDefeated(string bossId)
    {
        if (string.IsNullOrWhiteSpace(bossId) || defeatedBossIds.Contains(bossId))
        {
            return false;
        }

        defeatedBossIds.Add(bossId);
        return true;
    }

    public bool HasFlower(string flowerId)
    {
        return !string.IsNullOrWhiteSpace(flowerId) && collectedFlowerIds.Contains(flowerId);
    }

    public bool CollectFlower(string flowerId)
    {
        if (string.IsNullOrWhiteSpace(flowerId) || collectedFlowerIds.Contains(flowerId))
        {
            return false;
        }

        collectedFlowerIds.Add(flowerId);
        return true;
    }

    public bool HasCompletedArena(string arenaId)
    {
        return !string.IsNullOrWhiteSpace(arenaId) && completedArenaIds.Contains(arenaId);
    }

    public bool MarkArenaCompleted(string arenaId)
    {
        if (string.IsNullOrWhiteSpace(arenaId) || completedArenaIds.Contains(arenaId))
        {
            return false;
        }

        completedArenaIds.Add(arenaId);
        return true;
    }

    public bool HasDefeatedEnemy(string enemyId)
    {
        return !string.IsNullOrWhiteSpace(enemyId) && defeatedEnemyIds.Contains(enemyId);
    }

    public bool MarkEnemyDefeated(string enemyId)
    {
        if (string.IsNullOrWhiteSpace(enemyId) || defeatedEnemyIds.Contains(enemyId))
        {
            return false;
        }

        defeatedEnemyIds.Add(enemyId);
        return true;
    }

    public void LoadProgress(
        List<string> savedDefeatedBossIds,
        List<string> savedCollectedFlowerIds,
        List<string> savedCompletedArenaIds,
        List<string> savedDefeatedEnemyIds)
    {
        defeatedBossIds = savedDefeatedBossIds != null ? new List<string>(savedDefeatedBossIds) : new List<string>();
        collectedFlowerIds = savedCollectedFlowerIds != null ? new List<string>(savedCollectedFlowerIds) : new List<string>();
        completedArenaIds = savedCompletedArenaIds != null ? new List<string>(savedCompletedArenaIds) : new List<string>();
        defeatedEnemyIds = savedDefeatedEnemyIds != null ? new List<string>(savedDefeatedEnemyIds) : new List<string>();
        loadedFromSave = true;
    }

    private void TryAutoLoadFromSave()
    {
        if (loadedFromSave)
        {
            return;
        }

        if (!SaveSystem.TryLoad(out SaveData data) || data == null)
        {
            return;
        }

        LoadProgress(data.defeatedBossIds, data.collectedFlowerIds, data.completedArenaIds, data.defeatedEnemyIds);
        Debug.Log("💾 GameProgress auto-loaded dal save.");
    }
}
