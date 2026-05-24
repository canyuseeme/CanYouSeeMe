using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private GameObject Star1;
    private GameObject Star2;
    private GameObject Star3;

    [Header("Settings")]
    public int playerLives = 3;

    [Header("Star Tracking")]
    public bool star2Eligible = true; // 1 Bullet = 1 Kill
    public bool star3Eligible = true; // No Lives Lost
    private string levelID; // Automatically grabs Scene name

    [Header("Death Scene References")]
    public GameObject mainCanvas;      
    public GameObject blackPrefab;     
    public GameObject whitePlayerPrefab; 
    public GameObject activePlayer;    

    [Header("UI Elements")]
    public GameObject[] heartSprites;

    [Header("Sounds")]
    public AudioClip damageSound;
    [Range(0f, 1f)] public float volume = 0.6f;

    public AudioClip deathSound;
    [Range(0f, 1f)] public float volume2 = 1f;

    public AudioClip heartbeatSound;
    [Range(0f, 1f)] public float volume3 = 1f;
    private AudioSource heartbeatSource;

    public GameObject[] bulletSprites;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        heartbeatSource = gameObject.AddComponent<AudioSource>();
        heartbeatSource.clip = heartbeatSound;
        heartbeatSource.loop = true;
        heartbeatSource.playOnAwake = false;
        heartbeatSource.volume = volume3;

        FindStars();
    }

    void Start()
    {
        levelID = SceneManager.GetActiveScene().name;
    }

    void Update()
    {
        //Stars
        if (GameObject.Find("bulletHole(Clone)") != null)
        {
            DisableObject(Star2);
        }

        //Leave Level
        if (Keyboard.current.leftShiftKey.isPressed && Keyboard.current.rightShiftKey.isPressed)
        {
            SceneManager.LoadScene("StartScreen");
        }
        //Restart Level
        else if (Keyboard.current.enterKey.isPressed && Keyboard.current.rightShiftKey.isPressed)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void FindStars()
    {
        // This scans the entire scene, including inactive objects and children
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            // Ensure the object belongs to the active scene and isn't a project asset/prefab
            if (obj.hideFlags == HideFlags.None)
            {
                if (obj.name == "S1") Star1 = obj;
                else if (obj.name == "S2") Star2 = obj;
                else if (obj.name == "S3") Star3 = obj;
            }
        }

        // Quick debug check to verify they were found
        if (Star1 == null) Debug.LogWarning("Star1 (S1) not found in scene.");
        if (Star2 == null) Debug.LogWarning("Star2 (S2) not found in scene.");
        if (Star3 == null) Debug.LogWarning("Star3 (S3) not found in scene.");
    }
    private void DisableObject(GameObject target)
    {
        if (target != null)
        {
            target.SetActive(false);
        }
    }

    public void SaveStarData()
    {
        // 1. Star 1 is permanent once beaten
        PlayerPrefs.SetInt(levelID + "_Star1", 1);

        // 2. Star 2 (Accuracy): Only save if they earned it AND they don't already have it
        if (star2Eligible) 
        {
            PlayerPrefs.SetInt(levelID + "_Star2", 1);
        }

        // 3. Star 3 (No Damage): Only save if they earned it AND they don't already have it
        if (star3Eligible) 
        {
            PlayerPrefs.SetInt(levelID + "_Star3", 1);
        }

        // 4. Star 4 (Flawless): Only save if BOTH are true in THIS specific run
        if (star2Eligible && star3Eligible)
        {
            PlayerPrefs.SetInt(levelID + "_Star4", 1);
        }

        PlayerPrefs.Save(); 
    }

    public void LoseLife()
    {
        star3Eligible = false;
        DisableObject(Star3);

        if (damageSound != null) AudioSource.PlayClipAtPoint(damageSound, transform.position, volume);

        playerLives--;

        if (playerLives >= 0 && playerLives < heartSprites.Length)
        {
            heartSprites[playerLives].SetActive(false); 
        }

        if (playerLives == 1) StartHeartbeat();
        else if (playerLives <= 0)
        {
            StopHeartbeat();
            StartCoroutine(GameOver());
        }
    }

    void StartHeartbeat()
    {
        if (heartbeatSound != null && !heartbeatSource.isPlaying) heartbeatSource.Play();
    }

    void StopHeartbeat()
    {
        if (heartbeatSource.isPlaying) heartbeatSource.Stop();
    }

    public void UpdateAmmoUI(int currentAmmo)
    {
        for (int i = 0; i < bulletSprites.Length; i++)
        {
            bulletSprites[i].SetActive(i < currentAmmo);
        }
    }

    IEnumerator GameOver()
    {
        GameObject.Find("NR").GetComponent<AudioSource>().Stop();
        if (mainCanvas != null) mainCanvas.SetActive(false);

        Vector3 deathPosition = activePlayer.transform.position;
        Quaternion deathRotation = activePlayer.transform.rotation;

        if (activePlayer != null) Destroy(activePlayer);

        if (blackPrefab != null)
        {
            Vector3 blackPos = new Vector3(deathPosition.x, deathPosition.y, -8f);
            Instantiate(blackPrefab, blackPos, Quaternion.identity);
        }

        GameObject dummyPlayer = null;
        if (whitePlayerPrefab != null)
        {
            Vector3 dummyPos = new Vector3(deathPosition.x, deathPosition.y, -9f);
            dummyPlayer = Instantiate(whitePlayerPrefab, dummyPos, deathRotation);
        }

        yield return new WaitForSeconds(1.5f);
        if (deathSound != null) AudioSource.PlayClipAtPoint(deathSound, deathPosition, volume2);
        if (dummyPlayer != null) Destroy(dummyPlayer);
        
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}