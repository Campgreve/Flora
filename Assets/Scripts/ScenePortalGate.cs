using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ScenePortalGate : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string targetSceneName = "FireForest";

    [Header("Requirement")]
    [SerializeField] private bool requireBossDefeated = true;
    [SerializeField] private string requiredBossId = "boss_tutorial";

    [Header("Transition")]
    [SerializeField] private float fadeOutDuration = 0.4f;
    [SerializeField] private float fadeInDuration = 0.35f;
    [SerializeField] private bool saveBeforeTransition = true;

    [Header("Feedback")]
    [SerializeField] private string blockedSfx = "Error";

    private bool transitionStarted;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (transitionStarted || SceneTransitionManager.IsLoading)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (requireBossDefeated && !GameProgress.Instance.HasDefeatedBoss(requiredBossId))
        {
            if (!string.IsNullOrWhiteSpace(blockedSfx))
            {
                SoundEffectManager.Play(blockedSfx, true);
            }
            Debug.Log($"🚫 Portale bloccato: devi prima sconfiggere '{requiredBossId}'.");
            return;
        }

        if (saveBeforeTransition)
        {
            SaveSystem.SavePlayer(other.gameObject);
        }

        transitionStarted = true;
        SceneTransitionManager.LoadSceneWithFade(targetSceneName, fadeOutDuration, fadeInDuration);
    }
}
