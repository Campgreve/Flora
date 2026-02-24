using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FireForestEnemyEmitterBootstrap : MonoBehaviour
{
    [SerializeField] private string fireSceneName = "FireForest";
    [SerializeField] private float scanInterval = 1f;

    private Coroutine scanRoutine;

    private void OnEnable()
    {
        if (!IsFireScene())
        {
            return;
        }

        scanRoutine = StartCoroutine(ScanLoop());
    }

    private void OnDisable()
    {
        if (scanRoutine != null)
        {
            StopCoroutine(scanRoutine);
            scanRoutine = null;
        }
    }

    private bool IsFireScene()
    {
        return SceneManager.GetActiveScene().name.Equals(fireSceneName, System.StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerator ScanLoop()
    {
        while (true)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            for (int i = 0; i < enemies.Length; i++)
            {
                GameObject enemy = enemies[i];
                if (enemy == null || !enemy.activeInHierarchy)
                {
                    continue;
                }

                if (enemy.GetComponent<EnemyFireEmitter>() == null)
                {
                    enemy.AddComponent<EnemyFireEmitter>();
                }
            }

            yield return new WaitForSeconds(scanInterval);
        }
    }
}
