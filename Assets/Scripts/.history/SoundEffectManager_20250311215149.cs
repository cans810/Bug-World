using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundEffectManager : MonoBehaviour
{
    // Singleton instance
    public static SoundEffectManager Instance { get; private set; }
    
    [System.Serializable]
    public class SoundEffect
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 1.5f)] public float pitch = 1f;
        [Range(0f, 0.5f)] public float randomPitchVariation = 0.1f;
        public bool loop = false;
        [Range(0f, 1f)] public float spatialBlend = 0f; // 0 = 2D, 1 = 3D
        [Range(0f, 500f)] public float maxDistance = 50f;
        
        [HideInInspector] public AudioSource source;
    }
    
    [Header("Sound Effects")]
    [SerializeField] private List<SoundEffect> soundEffects = new List<SoundEffect>();
    
    [Header("Audio Settings")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private int audioSourcePoolSize = 10;
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [SerializeField] private bool muteAllSounds = false;
    
    // Pool of audio sources for playing multiple sounds simultaneously
    private List<AudioSource> audioSourcePool = new List<AudioSource>();
    
    // Dictionary for quick lookup of sound effects by name
    private Dictionary<string, SoundEffect> soundEffectDictionary = new Dictionary<string, SoundEffect>();
    
    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize the audio source pool
        InitializeAudioSourcePool();
        
        // Build the dictionary for quick lookup
        foreach (SoundEffect sound in soundEffects)
        {
            if (!soundEffectDictionary.ContainsKey(sound.name))
            {
                soundEffectDictionary.Add(sound.name, sound);
            }
            else
            {
                Debug.LogWarning($"Duplicate sound effect name: {sound.name}. Only the first one will be used.");
            }
        }
    }
    
    private void InitializeAudioSourcePool()
    {
        // Create a child object to hold all audio sources
        Transform audioSourceContainer = new GameObject("Audio Source Pool").transform;
        audioSourceContainer.SetParent(transform);
        
        // Create the pool of audio sources
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            GameObject audioSourceObj = new GameObject($"Audio Source {i}");
            audioSourceObj.transform.SetParent(audioSourceContainer);
            
            AudioSource source = audioSourceObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            
            if (sfxMixerGroup != null)
            {
                source.outputAudioMixerGroup = sfxMixerGroup;
            }
            
            audioSourcePool.Add(source);
        }
    }
    
    // Get an available audio source from the pool
    private AudioSource GetAvailableAudioSource()
    {
        // First, try to find an audio source that's not playing
        foreach (AudioSource source in audioSourcePool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }
        
        // If all sources are in use, find the one that started playing the longest time ago
        AudioSource oldestSource = audioSourcePool[0];
        float oldestPlayTime = float.MaxValue;
        
        foreach (AudioSource source in audioSourcePool)
        {
            // Get the time position in the clip
            float playTime = source.time;
            
            if (playTime < oldestPlayTime)
            {
                oldestPlayTime = playTime;
                oldestSource = source;
            }
        }
        
        // Stop the oldest source and return it
        oldestSource.Stop();
        return oldestSource;
    }
    
    // Play a sound effect by name
    public AudioSource PlaySound(string soundName)
    {
        return PlaySound(soundName, Vector3.zero, false);
    }
    
    // Play a sound effect at a specific position
    public AudioSource PlaySound(string soundName, Vector3 position, bool is3D = true)
    {
        // Check if sound exists
        if (!soundEffectDictionary.TryGetValue(soundName, out SoundEffect sound))
        {
            Debug.LogWarning($"Sound effect '{soundName}' not found!");
            return null;
        }
        
        // Don't play if muted
        if (muteAllSounds)
            return null;
        
        // Get an available audio source
        AudioSource audioSource = GetAvailableAudioSource();
        
        // Configure the audio source
        audioSource.clip = sound.clip;
        audioSource.volume = sound.volume * masterVolume;
        
        // Apply random pitch variation if specified
        float randomPitch = Random.Range(-sound.randomPitchVariation, sound.randomPitchVariation);
        audioSource.pitch = sound.pitch + randomPitch;
        
        audioSource.loop = sound.loop;
        
        // Set spatial settings
        if (is3D)
        {
            audioSource.spatialBlend = sound.spatialBlend;
            audioSource.maxDistance = sound.maxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.transform.position = position;
        }
        else
        {
            audioSource.spatialBlend = 0f; // 2D sound
        }
        
        // Play the sound
        audioSource.Play();
        
        return audioSource;
    }
    
    // Stop a specific sound
    public void StopSound(string soundName)
    {
        foreach (AudioSource source in audioSourcePool)
        {
            if (source.isPlaying && source.clip != null && source.clip.name == soundName)
            {
                source.Stop();
            }
        }
    }
    
    // Stop all sounds
    public void StopAllSounds()
    {
        foreach (AudioSource source in audioSourcePool)
        {
            if (source.isPlaying)
            {
                source.Stop();
            }
        }
    }
    
    // Set the master volume
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        // Update volume of all currently playing sounds
        foreach (AudioSource source in audioSourcePool)
        {
            if (source.isPlaying)
            {
                // Find the original sound effect to get its base volume
                foreach (SoundEffect sound in soundEffects)
                {
                    if (source.clip == sound.clip)
                    {
                        source.volume = sound.volume * masterVolume;
                        break;
                    }
                }
            }
        }
    }
    
    // Mute/unmute all sounds
    public void SetMute(bool mute)
    {
        muteAllSounds = mute;
        
        if (mute)
        {
            StopAllSounds();
        }
    }
    
    // Add a new sound effect at runtime
    public void AddSoundEffect(string name, AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (soundEffectDictionary.ContainsKey(name))
        {
            Debug.LogWarning($"Sound effect '{name}' already exists. Use UpdateSoundEffect instead.");
            return;
        }
        
        SoundEffect newSound = new SoundEffect
        {
            name = name,
            clip = clip,
            volume = volume,
            pitch = pitch,
            loop = loop
        };
        
        soundEffects.Add(newSound);
        soundEffectDictionary.Add(name, newSound);
    }
    
    // Update an existing sound effect
    public void UpdateSoundEffect(string name, AudioClip clip = null, float? volume = null, float? pitch = null, bool? loop = null)
    {
        if (!soundEffectDictionary.TryGetValue(name, out SoundEffect sound))
        {
            Debug.LogWarning($"Sound effect '{name}' not found. Cannot update.");
            return;
        }
        
        if (clip != null) sound.clip = clip;
        if (volume.HasValue) sound.volume = volume.Value;
        if (pitch.HasValue) sound.pitch = pitch.Value;
        if (loop.HasValue) sound.loop = loop.Value;
    }
} 