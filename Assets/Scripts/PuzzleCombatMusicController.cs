using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PuzzleCombatMusicController : MonoBehaviour
{
    [Header("Start Condition")]
    [SerializeField] private PressurePlate[] triggerPlates;

    [Header("Reward Condition")]
    [SerializeField] private BossFlowerPickup rewardPickup;
    [SerializeField] private string rewardFlowerId;

    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool forceColliderTrigger = true;

    private bool playerInsideArea;
    private bool combatLockActive;
    private bool rewardCollected;
    private bool firstPlateTriggered;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        if (forceColliderTrigger)
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(rewardFlowerId) && GameProgress.Instance.HasFlower(rewardFlowerId))
        {
            rewardCollected = true;
        }
    }

    private void Update()
    {
        if (!firstPlateTriggered && AreAllTriggerPlatesPressed())
        {
            firstPlateTriggered = true;
            ActivateCombatLock();
        }

        if (!rewardCollected && !string.IsNullOrWhiteSpace(rewardFlowerId) && GameProgress.Instance.HasFlower(rewardFlowerId))
        {
            rewardCollected = true;
        }

        if (!rewardCollected && rewardPickup == null && !string.IsNullOrWhiteSpace(rewardFlowerId))
        {
            rewardCollected = GameProgress.Instance.HasFlower(rewardFlowerId);
        }

        TryReleaseMusicLock();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInsideArea = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInsideArea = false;
        TryReleaseMusicLock();
    }

    public void NotifyRewardCollected()
    {
        rewardCollected = true;
        TryReleaseMusicLock();
    }

    private void ActivateCombatLock()
    {
        if (combatLockActive)
        {
            return;
        }

        MusicManager music = MusicManager.Instance;
        if (music == null)
        {
            return;
        }

        music.PushCombatLock();
        music.PlayCombat();
        combatLockActive = true;
        Debug.Log("🎵 Puzzle music lock attivato: Combat.");
    }

    private void TryReleaseMusicLock()
    {
        if (!combatLockActive || !rewardCollected || playerInsideArea)
        {
            return;
        }

        MusicManager music = MusicManager.Instance;
        if (music != null)
        {
            music.PopCombatLock();
            music.PlayChill();
        }

        combatLockActive = false;
        Debug.Log("🎵 Puzzle music lock rilasciato: Chill.");
    }

    private void OnDisable()
    {
        if (!combatLockActive)
        {
            return;
        }

        MusicManager music = MusicManager.Instance;
        if (music != null)
        {
            music.PopCombatLock();
        }

        combatLockActive = false;
    }

    private bool AreAllTriggerPlatesPressed()
    {
        if (triggerPlates == null || triggerPlates.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < triggerPlates.Length; i++)
        {
            PressurePlate plate = triggerPlates[i];
            if (plate == null || !plate.IsPressed)
            {
                return false;
            }
        }

        return true;
    }
}
