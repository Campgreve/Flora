using System.Collections.Generic;
using UnityEngine;

public class SoundEffectLibrary : MonoBehaviour
{
    [SerializeField] private SoundEffectGroup[] soundEffectGroups;

    private Dictionary<string, List<AudioClip>> soundDictionary;

    private void Awake()
    {
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        soundDictionary = new Dictionary<string, List<AudioClip>>();

        foreach (SoundEffectGroup group in soundEffectGroups)
        {
            soundDictionary[group.name] = group.audioClips;
        }
    }

    public AudioClip GetRandomClip(string name)
    {
        if (!soundDictionary.TryGetValue(name, out var clips))
            return null;

        if (clips.Count == 0)
            return null;

        return clips[Random.Range(0, clips.Count)];
    }
}

[System.Serializable]
public struct SoundEffectGroup
{
    public string name;
    public List<AudioClip> audioClips;
}
