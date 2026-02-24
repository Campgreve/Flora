using UnityEngine;
using UnityEngine.UI;

public class SoundEffectManager : MonoBehaviour
{
    public static SoundEffectManager Instance;

    [SerializeField] private Slider sfxSlider;

    private static SoundEffectLibrary soundEffectLibrary;

    private AudioSource audioSource;
    private AudioSource randomPitchAudioSource;
    private AudioSource voiceAudioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            AudioSource[] audioSources = GetComponents<AudioSource>();

            if (audioSources.Length < 2)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                randomPitchAudioSource = gameObject.AddComponent<AudioSource>();
            }
            else
            {
                audioSource = audioSources[0];
                randomPitchAudioSource = audioSources[1];
                voiceAudioSource = audioSources[2];
            }

            soundEffectLibrary = GetComponent<SoundEffectLibrary>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnValueChanged);
            OnValueChanged(sfxSlider.value);
        }
    }

    public static void Play(string soundName, bool randomPitch = false)
    {
        if (Instance == null) return;

        AudioClip audioClip = soundEffectLibrary.GetRandomClip(soundName);
        if (audioClip == null) return;

        if (randomPitch)
        {
            Instance.randomPitchAudioSource.pitch = Random.Range(0.9f, 1.3f);
            Instance.randomPitchAudioSource.PlayOneShot(audioClip);
        }
        else
        {
            Instance.audioSource.PlayOneShot(audioClip);
        }
    }

    public static void PlayVoice(AudioClip audioClip, float pitch = 1f)
    {
        Instance.voiceAudioSource.pitch = pitch;
        Instance.voiceAudioSource.PlayOneShot(audioClip);
    }

    private void OnValueChanged(float value)
    {
        audioSource.volume = value;
        randomPitchAudioSource.volume = value;
        voiceAudioSource.volume = value;
    }
}
