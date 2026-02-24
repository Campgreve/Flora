using UnityEngine;
using UnityEngine.SceneManagement;

public static class CheckpointManager
{
    private static bool hasCheckpoint;
    private static bool respawnRequested;
    private static Vector3 checkpointPosition;
    private static string checkpointSceneName;

    public static bool HasCheckpointInCurrentScene =>
        hasCheckpoint && checkpointSceneName == SceneManager.GetActiveScene().name;

    public static void RegisterCheckpoint(Vector3 worldPosition)
    {
        hasCheckpoint = true;
        checkpointPosition = worldPosition;
        checkpointSceneName = SceneManager.GetActiveScene().name;
    }

    public static void RequestRespawnAtCheckpoint()
    {
        respawnRequested = HasCheckpointInCurrentScene;
    }

    public static void CancelPendingRespawn()
    {
        respawnRequested = false;
    }

    public static bool TryGetRespawnPosition(out Vector3 worldPosition)
    {
        if (respawnRequested && HasCheckpointInCurrentScene)
        {
            respawnRequested = false;
            worldPosition = checkpointPosition;
            return true;
        }

        worldPosition = Vector3.zero;
        return false;
    }
}
