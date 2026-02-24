using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawn manager per nemici nel dungeon
/// Controlla densità e distribuzione
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnData
    {
        public GameObject enemyPrefab;
        [Range(0f, 1f)]
        public float spawnWeight = 0.5f; // Probabilità di spawn
        public int minRoomSize = 0; // Spawna solo in stanze >= questa dimensione
    }

    [Header("Enemy Prefabs")]
    public EnemySpawnData[] enemies;

    [Header("Spawn Settings")]
    [Range(0f, 1f)]
    [Tooltip("Densità globale nemici: 0 = nessuno, 1 = massimo")]
    public float globalSpawnRate = 0.15f;
    
    [Tooltip("Nemici per stanza piccola (10x10)")]
    public int baseEnemiesPerRoom = 2;
    
    [Tooltip("Moltiplicatore per stanze grandi")]
    public float largeRoomMultiplier = 1.5f;

    [Header("Distribution")]
    [Tooltip("Non spawna nemici nella stanza di spawn")]
    public bool avoidStartRoom = true;
    
    [Tooltip("Più nemici nelle stanze lontane dallo spawn")]
    public bool increaseWithDistance = true;
    
    public float distanceMultiplier = 0.02f;

    [Header("Combat Rooms")]
    [Tooltip("Moltiplicatore per stanze combat")]
    public float combatRoomMultiplier = 2.5f;

    private List<GameObject> spawnedEnemies = new List<GameObject>();

    /// <summary>
    /// Spawna nemici per una stanza specifica
    /// </summary>
    public void SpawnEnemiesInRoom(ProceduralMapGenerator.Room room, Vector3 playerSpawnPos, Transform parent = null)
    {
        if (enemies == null || enemies.Length == 0)
            return;

        // Skip stanza di spawn se richiesto
        if (avoidStartRoom && room.type == ProceduralMapGenerator.RoomType.Start)
            return;

        // Calcola quanti nemici spawna
        int enemyCount = CalculateEnemyCount(room, playerSpawnPos);

        for (int i = 0; i < enemyCount; i++)
        {
            // Scegli tipo nemico random (weighted)
            GameObject enemyPrefab = ChooseEnemyType(room);
            if (enemyPrefab == null)
                continue;

            // Trova posizione random nella stanza
            Vector3 spawnPos = GetRandomSpawnPosition(room);
            
            // Spawna
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, parent);
            spawnedEnemies.Add(enemy);
        }
    }

    int CalculateEnemyCount(ProceduralMapGenerator.Room room, Vector3 playerSpawnPos)
    {
        // Base count in base alla dimensione della stanza
        float roomArea = room.width * room.height;
        float baseArea = 10f * 10f; // Stanza di riferimento 10x10
        
        float count = baseEnemiesPerRoom * (roomArea / baseArea);

        // Moltiplicatore per stanze grandi
        if (room.type == ProceduralMapGenerator.RoomType.Large)
        {
            count *= largeRoomMultiplier;
        }

        // Moltiplicatore per combat room
        if (room.type == ProceduralMapGenerator.RoomType.Combat)
        {
            count *= combatRoomMultiplier;
        }

        // Aumenta con distanza dallo spawn
        if (increaseWithDistance)
        {
            float distance = Vector2.Distance(room.Center(), new Vector2(playerSpawnPos.x, playerSpawnPos.y));
            count *= (1f + distance * distanceMultiplier);
        }

        // Applica global spawn rate
        count *= globalSpawnRate;

        // Casualità ±30%
        count *= Random.Range(0.7f, 1.3f);

        return Mathf.RoundToInt(Mathf.Max(0, count));
    }

    GameObject ChooseEnemyType(ProceduralMapGenerator.Room room)
    {
        // Filtra nemici che possono spawna in questa stanza
        List<EnemySpawnData> validEnemies = new List<EnemySpawnData>();
        
        foreach (var enemy in enemies)
        {
            if (enemy.enemyPrefab == null)
                continue;
            
            // Check dimensione minima stanza
            int roomSize = Mathf.Min(room.width, room.height);
            if (roomSize >= enemy.minRoomSize)
            {
                validEnemies.Add(enemy);
            }
        }

        if (validEnemies.Count == 0)
            return null;

        // Weighted random selection
        float totalWeight = 0f;
        foreach (var enemy in validEnemies)
        {
            totalWeight += enemy.spawnWeight;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var enemy in validEnemies)
        {
            currentWeight += enemy.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return enemy.enemyPrefab;
            }
        }

        return validEnemies[0].enemyPrefab;
    }

    Vector3 GetRandomSpawnPosition(ProceduralMapGenerator.Room room)
    {
        // Evita i bordi della stanza
        int margin = 3;
        
        int x = Random.Range(room.x + margin, room.x + room.width - margin);
        int y = Random.Range(room.y + margin, room.y + room.height - margin);

        // Converti in world position (assumendo tile size 1x1)
        return new Vector3(x + 0.5f, y + 0.5f, 0f);
    }

    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        spawnedEnemies.Clear();
    }

    public int GetEnemyCount()
    {
        return spawnedEnemies.Count;
    }
}