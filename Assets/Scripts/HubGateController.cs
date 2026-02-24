using System.Collections.Generic;
using UnityEngine;

public class HubGateController : MonoBehaviour
{
    [Header("Required Levers")]
    [SerializeField] private List<LeverSwitch> levers = new List<LeverSwitch>();

    [Header("Gate")]
    [SerializeField] private GameObject gateRoot;
    [SerializeField] private Collider2D gateCollider;
    [SerializeField] private SpriteRenderer gateRenderer;
    [SerializeField] private bool hideGateWhenOpen = true;

    [Header("Hub Light / Feedback")]
    [SerializeField] private SpriteRenderer hubLightRenderer;
    [SerializeField] private Color lightOffColor = new Color(1f, 0.8f, 0.3f, 0.15f);
    [SerializeField] private Color lightOnColor = new Color(1f, 0.72f, 0.25f, 1f);
    [SerializeField] private float lightOffScale = 0.65f;
    [SerializeField] private float lightOnScale = 1.35f;
    [SerializeField] private GameObject gateOpenFxPrefab;

    [Header("Audio")]
    [SerializeField] private string gateOpenSfx = "DoorSlam";

    private bool gateOpened;

    private void Start()
    {
        for (int i = 0; i < levers.Count; i++)
        {
            if (levers[i] != null)
            {
                levers[i].SetHubController(this);
            }
        }

        UpdateHubFeedback();
        TryOpenGateIfReady();
    }

    public void NotifyLeverActivated(LeverSwitch lever)
    {
        UpdateHubFeedback();
        TryOpenGateIfReady();
    }

    public int GetActivatedCount()
    {
        int count = 0;
        for (int i = 0; i < levers.Count; i++)
        {
            LeverSwitch lever = levers[i];
            if (lever != null && lever.IsActivated)
            {
                count++;
            }
        }

        return count;
    }

    public int GetRequiredCount()
    {
        int count = 0;
        for (int i = 0; i < levers.Count; i++)
        {
            if (levers[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private void TryOpenGateIfReady()
    {
        if (gateOpened)
        {
            return;
        }

        int required = GetRequiredCount();
        if (required <= 0)
        {
            return;
        }

        if (GetActivatedCount() < required)
        {
            return;
        }

        gateOpened = true;
        OpenGate();
    }

    private void OpenGate()
    {
        if (gateCollider != null)
        {
            gateCollider.enabled = false;
        }

        if (gateRenderer != null)
        {
            gateRenderer.enabled = !hideGateWhenOpen;
        }

        if (gateRoot != null && hideGateWhenOpen)
        {
            gateRoot.SetActive(false);
        }

        if (gateOpenFxPrefab != null)
        {
            Instantiate(gateOpenFxPrefab, transform.position, Quaternion.identity);
        }

        if (!string.IsNullOrWhiteSpace(gateOpenSfx))
        {
            SoundEffectManager.Play(gateOpenSfx, true);
        }

        Debug.Log("✅ Tutte le leve attive: cancello boss aperto.");
    }

    private void UpdateHubFeedback()
    {
        if (hubLightRenderer == null)
        {
            return;
        }

        int required = Mathf.Max(1, GetRequiredCount());
        float ratio = Mathf.Clamp01((float)GetActivatedCount() / required);

        hubLightRenderer.color = Color.Lerp(lightOffColor, lightOnColor, ratio);
        float scale = Mathf.Lerp(lightOffScale, lightOnScale, ratio);
        hubLightRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }
}
