using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public enum TutorialPhase { Circles, RedDots, Finished }

    [Header("Maps")]
    public GameObject map1;
    public GameObject map2;
    public GameObject map3;

    public GameObject lv1;
    public GameObject lv2;
    public GameObject lv3;

    [Header("Current Phase")]
    public TutorialPhase currentPhase = TutorialPhase.Circles;

    [Header("Phase 1: Circles Setup")]
    public Circles[] circles;
    private int currentCircleIndex = 0;

    [Header("Phase 2: Red Dots Setup")]
    public Dots redDotsPhase;

    [Header("Mentor Connection")]
    public Mentor mentor;
    public MentorDirection[] directions;

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

        TriggerDirection(0);
    }

    void Update()
    {
        switch (currentPhase)
        {
            case TutorialPhase.Circles:
                map1.gameObject.SetActive(true);
                lv1.gameObject.SetActive(true);
                HandleCirclesPhase();
                break;

            case TutorialPhase.RedDots:
                map1.gameObject.SetActive(false);
                lv1.gameObject.SetActive(false);
                map2.gameObject.SetActive(true);
                lv2.gameObject.SetActive(true);
                HandleRedDotsPhase();
                break;

            case TutorialPhase.Finished:
                map2.gameObject.SetActive(false);
                lv2.gameObject.SetActive(false);
                map3.gameObject.SetActive(true);
                lv3.gameObject.SetActive(true);
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

                TriggerDirection(currentCircleIndex);
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

    private void TriggerDirection(int directionIndex)
    {
        if (mentor == null) return;
        
        // Safety check to ensure you filled out enough directions in the inspector array
        if (directions != null && directionIndex < directions.Length)
        {
            mentor.ExecuteDirection(directions[directionIndex].position, directions[directionIndex].message);
        }
        else
        {
            Debug.LogWarning($"TutorialManager: Tried to trigger direction index {directionIndex}, but it hasn't been set up in the Directions array!");
        }
    }
}

[System.Serializable]
public class MentorDirection
{
    public UnityEngine.Transform position;
    [UnityEngine.TextArea(3, 5)] public string message;
}