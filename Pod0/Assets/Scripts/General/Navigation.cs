using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class Navigation : MonoBehaviour
{
    public string sceneToLoad;
    
    void Start()
    {
        //Select proper first level
        if (gameObject.name == "FIRST")
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    public void LoadLevel()
    {
        if (sceneToLoad == "QUIT") Application.Quit();

        SceneManager.LoadScene(sceneToLoad);
    }
}