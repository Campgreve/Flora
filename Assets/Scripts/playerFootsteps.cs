using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("Movement Detection")]
    [SerializeField] private float minSpeedToWalk = 0.1f; // Velocità minima per considerare che sta camminando
    
    [Header("Audio")]
    [SerializeField] private AudioClip footstepLoopSound; // Un singolo suono che si ripete in loop
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;
    [SerializeField] private float footstepVolume = 0.4f;
    [SerializeField] private float fadeSpeed = 5f; // Velocità di fade in/out
    
    
    private AudioSource audioSource;
    private Vector2 lastPosition;
    private bool isMoving;
    private float targetVolume;

    private void Start()
    {
        lastPosition = transform.position;
        
        // Crea e configura l'AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = footstepLoopSound;
        audioSource.loop = true; // Loop continuo
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // Suono 2D
        audioSource.volume = 0f; // Parte da silenzio
        
        // Pitch casuale iniziale
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        
        targetVolume = 0f;
    }

    private void Update()
    {
        // Controlla se il player si sta muovendo
        Vector2 currentPosition = transform.position;
        float speed = ((currentPosition - lastPosition) / Time.deltaTime).magnitude;
        isMoving = speed > minSpeedToWalk;
        lastPosition = currentPosition;
        
        // Gestisci l'audio
        if (isMoving)
        {
            // Avvia il suono se non sta già suonando
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                // Cambia leggermente il pitch ogni volta che riprende
                audioSource.pitch = Random.Range(minPitch, maxPitch);
            }
            
            targetVolume = footstepVolume;
            
        }
        else
        {
            targetVolume = 0f;
        }
        
        // Fade in/out del volume
        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, fadeSpeed * Time.deltaTime);
        
        // Ferma completamente l'audio quando il volume è quasi zero
        if (audioSource.volume < 0.01f && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private void OnDestroy()
    {
        // Pulisci l'AudioSource quando il player viene distrutto
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}
