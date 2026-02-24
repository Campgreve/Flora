using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArenaController : MonoBehaviour
{
    private static int activeArenaCount = 0;
    public static bool AnyArenaActive => activeArenaCount > 0;

    [Header("Arena Settings")]
    public GameObject barrier; // La barriera che blocca l'uscita
    
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] enemyPrefabs; // Prefab dei nemici da spawnare
    [SerializeField] private Transform[] spawnPoints; // Punti dove spawnare
    [SerializeField] private int enemiesPerWave = 5;
    [SerializeField] private float timeBetweenSpawns = 0.5f;
    
    [Header("Wave Settings")]
    [SerializeField] private int totalWaves = 3;
    [SerializeField] private float timeBetweenWaves = 3f;
    [SerializeField] private bool increaseEnemiesPerWave = true;
    [SerializeField] private int enemyIncreasePerWave = 2;
    
    [Header("Completion")]
    [SerializeField] private bool oneTimeArena = true;
    [SerializeField] private string arenaId = "";
    
    [Header("Optional - Visual Feedback")]
    public AudioClip barrierActivateSound;
    public AudioClip barrierDeactivateSound;
    
    private List<GameObject> enemies = new List<GameObject>();
    private bool arenaActive = false;
    private bool arenaCompleted = false;
    private bool countedAsActive = false;
    private int currentWave = 0;
    private AudioSource audioSource;
    
    void Start()
    {
        if (string.IsNullOrWhiteSpace(arenaId))
        {
            Vector3 p = transform.position;
            arenaId = $"{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}_arena_{Mathf.RoundToInt(p.x * 10f)}_{Mathf.RoundToInt(p.y * 10f)}";
        }

        if (oneTimeArena && GameProgress.Instance.HasCompletedArena(arenaId))
        {
            arenaCompleted = true;
            Collider2D triggerCol = GetComponent<Collider2D>();
            if (triggerCol != null) triggerCol.enabled = false;
        }

        if (barrier != null)
        {
            barrier.SetActive(false);
        }
        
        audioSource = GetComponent<AudioSource>();
        
        // Nascondi gli spawn points
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
                point.gameObject.SetActive(false);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !arenaActive && (!oneTimeArena || !arenaCompleted))
        {
            ActivateArena();
        }
    }
    
    void ActivateArena()
    {
        if (oneTimeArena && arenaCompleted)
        {
            return;
        }

        arenaActive = true;
        currentWave = 0;
        enemies.Clear();

        if (!countedAsActive)
        {
            activeArenaCount++;
            countedAsActive = true;
        }
        
        if (barrier != null)
        {
            barrier.SetActive(true);
        }
        
        // Feedback audio
        if (audioSource != null && barrierActivateSound != null)
        {
            audioSource.PlayOneShot(barrierActivateSound);
        }
        
        // Passa alla musica di combattimento
        MusicManager.Instance.PlayCombat();
        
        Debug.Log("Arena attivata!");
        
        // Inizia le ondate
        StartCoroutine(SpawnWaves());
    }
    
    private IEnumerator SpawnWaves()
    {
        while (currentWave < totalWaves)
        {
            currentWave++;
            Debug.Log($"Ondata {currentWave}/{totalWaves} in arrivo!");
            
            int enemiesToSpawn = enemiesPerWave;
            if (increaseEnemiesPerWave)
            {
                enemiesToSpawn += (currentWave - 1) * enemyIncreasePerWave;
            }
            
            // Spawna i nemici dell'ondata
            yield return StartCoroutine(SpawnWave(enemiesToSpawn));
            
            // Aspetta che tutti i nemici siano morti prima della prossima wave.
            // Puliamo anche i riferimenti null (destroy esterni/non notificati).
            yield return new WaitUntil(() =>
            {
                enemies.RemoveAll(e => e == null);
                return enemies.Count == 0;
            });
            
            Debug.Log($"Ondata {currentWave} completata!");
            
            // Pausa tra le ondate
            if (currentWave < totalWaves)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }
        
        // Tutte le ondate completate
        DeactivateArena();
    }
    
    private IEnumerator SpawnWave(int enemyCount)
    {
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }
    
    private void SpawnEnemy()
    {
        if (spawnPoints.Length == 0 || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("Nessun spawn point o prefab nemico assegnato!");
            return;
        }
        
        // Scegli punto spawn casuale
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        // Scegli nemico casuale
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        
        // Spawna il nemico
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        // I nemici delle wave non devono usare persistenza per-ID,
        // altrimenti le wave successive vengono "filtrate" come già sconfitte.
        Enemy batEnemy = enemy.GetComponent<Enemy>();
        if (batEnemy != null)
        {
            batEnemy.SetPersistenceEnabled(false);
        }

        MushroomEnemy mushroomEnemy = enemy.GetComponent<MushroomEnemy>();
        if (mushroomEnemy != null)
        {
            mushroomEnemy.SetPersistenceEnabled(false);
        }

        enemies.Add(enemy);
    }
    
    // Questo viene chiamato da Enemy.cs quando muore
    public void OnEnemyDied(GameObject enemy)
    {
        if (!arenaActive) return;
        
        // Rimuovi il nemico dalla lista
        enemies.Remove(enemy);
        enemies.RemoveAll(e => e == null);
        
        Debug.Log($"Nemici rimasti: {enemies.Count}");
    }
    
    void DeactivateArena()
    {
        arenaActive = false;
        arenaCompleted = true;
        if (oneTimeArena)
        {
            GameProgress.Instance.MarkArenaCompleted(arenaId);
        }

        if (countedAsActive)
        {
            activeArenaCount = Mathf.Max(0, activeArenaCount - 1);
            countedAsActive = false;
        }
        
        if (barrier != null)
        {
            barrier.SetActive(false);
        }
        
        // Feedback audio
        if (audioSource != null && barrierDeactivateSound != null)
        {
            audioSource.PlayOneShot(barrierDeactivateSound);
        }
        
        // Torna alla musica chill
        MusicManager.Instance.PlayChill();
        
        Debug.Log("Arena completata! Puoi passare!");

        if (oneTimeArena)
        {
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null)
            {
                trigger.enabled = false;
            }
        }
    }
    
    // Reset arena (utile per debug o restart)
    public void ResetArena()
    {
        StopAllCoroutines();
        
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        
        enemies.Clear();
        currentWave = 0;
        arenaActive = false;
        arenaCompleted = false;

        if (countedAsActive)
        {
            activeArenaCount = Mathf.Max(0, activeArenaCount - 1);
            countedAsActive = false;
        }
        
        if (barrier != null)
            barrier.SetActive(false);

        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
            trigger.enabled = true;
    }
    
    // Visualizza spawn points nell'editor
    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;
        
        Gizmos.color = Color.red;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 0.5f);
                Gizmos.DrawLine(point.position, point.position + Vector3.up * 2f);
            }
        }
    }

    private void OnDisable()
    {
        if (countedAsActive)
        {
            activeArenaCount = Mathf.Max(0, activeArenaCount - 1);
            countedAsActive = false;
        }
    }
}
