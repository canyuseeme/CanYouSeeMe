using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class EnemyCounter : MonoBehaviour
{
    [Header("Settings")]
    public string enemyTag = "Enemy";
    public string winSceneName = "WinScreen";

    [Header("UI Reference")]
    public TextMeshProUGUI enemyCounterText;

    [Header("Portal Spawn")]
    public GameObject objectToSpawn; 
    public Transform spawnLocation; 

    private int totalEnemies;
    private GameObject[] remainingEnemies;
    private bool hasProcessedWin = false; 

    void Start()
    {
        totalEnemies = CheckEnemyCount();
    }

    void Update()
    {
        if (!hasProcessedWin)
        {
            CheckEnemyCount();
        }
    }

    int CheckEnemyCount()
    {
        remainingEnemies = GameObject.FindGameObjectsWithTag(enemyTag);
        int count = remainingEnemies.Length;

        DisplayCount(count);

        if (count <= 0)
        {
            hasProcessedWin = true; 
            
            // 1. SAVE THE DATA FIRST
            if (GameManager.Instance != null) GameManager.Instance.SaveStarData();

            // 2. SHOW LOGS
            ProcessStarResults();   

            // 3. SPAWN PORTAL
            SpawnWinObject();
            
            Invoke("Cleanup", 0.1f); 
        }

        return count;
    }

    void ProcessStarResults()
    {
        if (GameManager.Instance == null) return;

        bool s2 = GameManager.Instance.star2Eligible;
        bool s3 = GameManager.Instance.star3Eligible;
    }

    void SpawnWinObject()
    {
        if (objectToSpawn != null) Instantiate(objectToSpawn, spawnLocation.position, spawnLocation.rotation);
    }

    void DisplayCount(int currentCount)
    {
        if (enemyCounterText != null) enemyCounterText.text = currentCount.ToString() + "/" + totalEnemies.ToString();
    }

    void Cleanup()
    {
        Destroy(gameObject);
    }
}