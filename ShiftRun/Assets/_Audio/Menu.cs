using UnityEngine;

public class Menu : MonoBehaviour
{
    public GameObject menuUI;
    public GameObject player;

    void Start()
    {
        player.SetActive(false);
    }

    public void StartGame()
    {
        menuUI.SetActive(false);
        player.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}