using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SinkPurchaseSFX : MonoBehaviour
{
    [Serializable]
    public struct NodeSfxEntry
    {
        public string nodeId;
        public AudioClip clip;
    }

    [Header("Per-node SFX (highest priority)")]
    public NodeSfxEntry[] nodeSfx;

    [Header("Per-branch fallback SFX")]
    public AudioClip powerWasherClip;
    public AudioClip washBasinClip;
    public AudioClip dishwasherClip;

    [Header("Default fallback")]
    public AudioClip defaultClip;

    private AudioSource localSource;
    private SinkManager sinkManager;

    private void Awake()
    {
        localSource = GetComponent<AudioSource>();
        if (localSource == null) localSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        // Try to get singleton; fallback to FindAnyObjectByType if needed.
        sinkManager = SinkManager.Instance ?? FindAnyObjectByType<SinkManager>();
        if (sinkManager != null)
        {
            sinkManager.OnNodePurchased += HandleNodePurchased;
        }
        else
        {
            Debug.LogWarning("[SinkPurchaseSFX] SinkManager not found on Start.");
        }
    }

    private void OnDestroy()
    {
        if (sinkManager != null)
            sinkManager.OnNodePurchased -= HandleNodePurchased;
    }

    private void HandleNodePurchased(string nodeId)
    {
        AudioClip clip = GetClipForNode(nodeId);
        if (clip == null) return;

        // Use centralized AudioManager when present, otherwise use local source
        if (AudioManager.instance != null)
        {
            try
            {
                AudioManager.instance.PlaySFX(clip);
                return;
            }
            catch (Exception) { /* fall through to local playback */ }
        }

        if (localSource != null)
            localSource.PlayOneShot(clip);
    }

    private AudioClip GetClipForNode(string nodeId)
    {
        if (!string.IsNullOrEmpty(nodeId) && nodeSfx != null)
        {
            for (int i = 0; i < nodeSfx.Length; i++)
            {
                if (string.Equals(nodeSfx[i].nodeId, nodeId, StringComparison.OrdinalIgnoreCase))
                {
                    if (nodeSfx[i].clip != null) return nodeSfx[i].clip;
                }
            }
        }

        // If no per-node clip, try branch fallback
        if (sinkManager != null)
        {
            var node = sinkManager.GetNode(nodeId);
            if (node != null)
            {
                switch (node.branch)
                {
                    case SinkManager.SinkType.PowerWasher:
                        if (powerWasherClip != null) return powerWasherClip;
                        break;
                    case SinkManager.SinkType.WashBasin:
                        if (washBasinClip != null) return washBasinClip;
                        break;
                    case SinkManager.SinkType.Dishwasher:
                        if (dishwasherClip != null) return dishwasherClip;
                        break;
                }
            }
        }

        // Last resort
        return defaultClip;
    }
}