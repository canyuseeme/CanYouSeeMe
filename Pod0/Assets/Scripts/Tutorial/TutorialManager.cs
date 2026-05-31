using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public enum TutorialPhase { Circles, RedDots, Finished }

    [Header("Current Phase")]
    public TutorialPhase currentPhase = TutorialPhase.Circles;

    [Header("Phase 1: Circles Setup")]
    public Circles[] circles;
    private int currentCircleIndex = 0;

    [Header("Phase 2: Red Dots Setup")]
    public Dots redDotsPhase;

    void Start()
    {
        // Initialize Phase 1: Turn on first circle, turn off the rest
        for (int i = 0; i < circles.Length; i++)
        {
            if (circles[i] != null) circles[i].gameObject.SetActive(i == 0);
        }

        // Keep the RedDots completely hidden/disabled at startup
        if (redDotsPhase != null)
        {
            redDotsPhase.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        switch (currentPhase)
        {
            case TutorialPhase.Circles:
                HandleCirclesPhase();
                break;

            case TutorialPhase.RedDots:
                HandleRedDotsPhase();
                break;

            case TutorialPhase.Finished:
                // Stop running tracking code once everything is completely clear!
                break;
        }
    }

    void HandleCirclesPhase()
    {
        if (currentCircleIndex >= circles.Length) return;

        Circles activeCircle = circles[currentCircleIndex];

        if (activeCircle != null && activeCircle.IsCompleted)
        {
            activeCircle.gameObject.SetActive(false);
            currentCircleIndex++;

            if (currentCircleIndex < circles.Length && circles[currentCircleIndex] != null)
            {
                circles[currentCircleIndex].gameObject.SetActive(true);
            }
            else
            {
                // ALL CIRCLES CLEARED -> Transition directly to Red Dots!
                Debug.Log("TutorialManager: Circle phase complete. Switching to Red Dots Phase!");
                currentPhase = TutorialPhase.RedDots;
                
                if (redDotsPhase != null)
                {
                    redDotsPhase.gameObject.SetActive(true);
                }
            }
        }
    }

    void HandleRedDotsPhase()
    {
        if (redDotsPhase != null && redDotsPhase.IsCompleted)
        {
            redDotsPhase.gameObject.SetActive(false);
            
            // Transition to Phase 3 (Obstacles / Enemies)
            currentPhase = TutorialPhase.Finished;
            Debug.Log("TutorialManager: Red Dots phase complete! Ready for obstacles.");
            
            // You can easily activate an obstacle gameobject or trigger your next phase script here!
        }
    }
}