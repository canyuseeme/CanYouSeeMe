using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class Intro : MonoBehaviour
{
    [Header("Slide Settings")]
    public float moveDistance = 5f;
    public float timePerSlide = 3.0f;
    public float transitionSpeed = 0.5f;
    public int totalSlides = 6;
    public string nextSceneName = "Intro";

    [Header("References")]
    public Transform cameraTransform;

    private int currentSlide = 0;
    private bool isTransitioning = false;

    [Header("Sound")]
    public AudioClip introSound1;
    public AudioClip introSound2;
    [Range(0f, 1f)] public float volume = 1f;
    private int Switch = 0;

    void Start()
    {
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        StartCoroutine(PlayStory());
    }

    void Update()
    {
        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            LoadNextScene();
        }
    }

    IEnumerator PlayStory()
    {
        while (currentSlide < totalSlides - 1)
        {
            // 1. Wait on the current slide
            yield return new WaitForSeconds(timePerSlide);
            
            if (Switch == 1)
            {
                if (introSound2 != null)  AudioSource.PlayClipAtPoint(introSound2, cameraTransform.position, volume);
                Switch = 0;
            }
            else
            {
                if (introSound1 != null)  AudioSource.PlayClipAtPoint(introSound1, cameraTransform.position, volume);
                Switch = 1;
            }
            
            // 2. Smoothly slide to the next one
            Vector3 startPos = cameraTransform.position;
            Vector3 endPos = startPos + new Vector3(moveDistance, 0, 0);
            
            float elapsed = 0f;
            while (elapsed < transitionSpeed)
            {
                // Smoothly interpolate between start and end position
                cameraTransform.position = Vector3.Lerp(startPos, endPos, elapsed / transitionSpeed);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 3. Ensure we land exactly on the target
            cameraTransform.position = endPos;
            currentSlide++;
        }

        yield return new WaitForSeconds(timePerSlide);
        LoadNextScene();
    }

    void LoadNextScene()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        SceneManager.LoadScene(nextSceneName);
    }
}