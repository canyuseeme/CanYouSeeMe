using UnityEngine;
using System.Collections.Generic;

public class LevelSet2 : MonoBehaviour
{   
    public List<GameObject> objects = new List<GameObject>();
    private bool visible = false;

    void Start()
    {
        if (PlayerPrefs.GetInt("Level1_Star1", 0) == 1 && PlayerPrefs.GetInt("Level2_Star1", 0) == 1 && PlayerPrefs.GetInt("Level3_Star1", 0) == 1 && PlayerPrefs.GetInt("Level4_Star1", 0) == 1 && PlayerPrefs.GetInt("Level5_Star1", 0) == 1)
        {
            visible = true;
        }
        
        foreach (GameObject obj in objects)
        {
            if (obj != null) obj.SetActive(visible);
        }
    }

    void Update()
    {
        
    }
}
