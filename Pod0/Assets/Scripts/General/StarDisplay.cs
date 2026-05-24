using UnityEngine;

public partial class LevelStarDisplay : MonoBehaviour
{
    [Header("Which Level is this for?")]
    public string levelID = "Level_01"; 

    [Header("Star Prefabs")]
    public GameObject star1Prefab; 
    public GameObject star2Prefab; 
    public GameObject star3Prefab; 
    public GameObject star4Prefab; 

    [Header("Location Markers")]
    public Transform location1;
    public Transform location2;
    public Transform location3;

    void Start()
    {
        UpdateStarVisuals();
    }

    public void UpdateStarVisuals()
    {
        int s1 = PlayerPrefs.GetInt(levelID + "_Star1", 0);
        int s2 = PlayerPrefs.GetInt(levelID + "_Star2", 0);
        int s3 = PlayerPrefs.GetInt(levelID + "_Star3", 0);
        int s4 = PlayerPrefs.GetInt(levelID + "_Star4", 0);

        // ALWAYS clear old objects first to prevent overlaps
        ClearLocation(location1);
        ClearLocation(location2);
        ClearLocation(location3);

        // --- THE "NEVER LOSE IT" LOGIC ---
        
        // 1. If Star 4 (Flawless) was EVER earned, it stays forever.
        if (s4 == 1)
        {
            SpawnStar(star4Prefab, location1);
            SpawnStar(star4Prefab, location2);
            SpawnStar(star4Prefab, location3);
            return; // EXIT EARLY. If you are flawless, we don't care about individual stars.
        }

        // 2. Otherwise, check each star INDEPENDENTLY from the Save File.
        // If s2 was a '1' from a run 20 minutes ago, it is still a '1' now.
        if (s1 == 1) SpawnStar(star1Prefab, location1);
        if (s2 == 1) SpawnStar(star2Prefab, location2);
        if (s3 == 1) SpawnStar(star3Prefab, location3);
    }

    void SpawnStar(GameObject prefab, Transform parentLocation)
    {
        if (parentLocation == null || prefab == null) return;
        Instantiate(prefab, parentLocation.position, parentLocation.rotation, parentLocation);
    }

    void ClearLocation(Transform location)
    {
        if (location == null) return;
        foreach (Transform child in location)
        {
            Destroy(child.gameObject);
        }
    }
}