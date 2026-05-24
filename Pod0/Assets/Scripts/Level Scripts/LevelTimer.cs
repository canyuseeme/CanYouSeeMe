using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelTimer : MonoBehaviour
{
    [Header("Settings")]
    public float timeRemaining = 60f;
    public string loseSceneName = "LoseScreen";

    [Header("UI Reference")]
    public TextMeshProUGUI timerText;

    private bool isTimerRunning = false;

    void Start()
    {
        // Starts the timer when the level begins
        isTimerRunning = true;
    }

    void Update()
    {
        if (isTimerRunning)
        {
            if (timeRemaining > 0.1)
            {
                // Time.deltaTime is the time since the last frame
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else
            {
                Debug.Log("Time has run out!");
                timeRemaining = 0;
                isTimerRunning = false;
                GameOver();
            }
        }
    }

    void DisplayTime(float timeToDisplay)
    {
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void GameOver()
    {
        SceneManager.LoadScene(loseSceneName);
    }
}