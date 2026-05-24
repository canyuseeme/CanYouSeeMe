using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Type the exact name of the scene you want to load.")]
    [SerializeField] private string sceneName;
    
    public AudioClip selectSound;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            LoadLevel();
        }
    }

    public void LoadLevel()
    {
        if (sceneName == "QUIT")
        {
            Application.Quit();
        }
        else if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}