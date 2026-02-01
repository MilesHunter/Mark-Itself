using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUIManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void startDraw()
    {
        SceneManager.LoadScene("uiscene");
    }

    public void exitGame()
    {
        // UnityEditor.EditorApplication.isPlaying = false;
        Application.Quit();
    }
}