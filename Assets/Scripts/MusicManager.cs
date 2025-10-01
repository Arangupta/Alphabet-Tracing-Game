using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public AudioSource backgroundSource;
    public AudioClip backgroundMusic;

    private static MusicManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Keeps it alive across scenes
            PlayMusic();
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    void PlayMusic()
    {
        if (backgroundSource != null && backgroundMusic != null)
        {
            backgroundSource.clip = backgroundMusic;
            backgroundSource.loop = true;
            backgroundSource.Play();
        }
    }
}
