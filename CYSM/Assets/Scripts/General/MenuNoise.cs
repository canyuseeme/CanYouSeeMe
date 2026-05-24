using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuNoise : MonoBehaviour
{
    public AudioClip selectSound;
    public AudioClip moveSound;
    [Range(0f, 1f)] public float volume = 1f;
    public AudioClip music;
    [Range(0f, 1f)] public float volume2 = 0.5f;
    private AudioSource musicSource;

    void Start()
    {
        PlaySelectSound();
        PlayMusic();
    }

    void Update()
    {
        if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            PlayMoveSound();
        }

        //Return to home screen
        if (Keyboard.current.leftShiftKey.isPressed && Keyboard.current.rightShiftKey.isPressed)
        {
            SceneManager.LoadScene("StartScreen");
        }
    }

    void PlaySelectSound()
    {
        if (selectSound != null)
        {
            AudioSource.PlayClipAtPoint(selectSound, transform.position, volume);
        }
    }

    void PlayMoveSound()
    {
        if (moveSound != null)
        {
            AudioSource.PlayClipAtPoint(moveSound, transform.position, volume);
        }
    }

    void PlayMusic()
    {
        if (music != null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = music;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = volume2;
            musicSource.Play();
        }
    }
}