using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SclectUIManager : MonoBehaviour
{
    // World UI
    public Button Button_EndGame;
    public Button Button_GameTutorial;
    public Button Button_SinglePlayer;
    public Button Button_OnlineGame;
    public Button Button_Setting;

    // Screen UI
    public GameObject Image_OnlineGame;
    public Button Button_ExitOnlineGame;
    public Button Button_CreateGame;
    public Button Button_AddGame;

    // Start is called before the first frame update
    void Start()
    {
        Button_EndGame.onClick.AddListener(()=>OnClickEndGame());
        Button_GameTutorial.onClick.AddListener(()=>OnClickSinglePlayer());
        Button_OnlineGame.onClick.AddListener(()=>OnClickOnlineGame());
        Button_Setting.onClick.AddListener(()=>OnClickSetting());


        Button_ExitOnlineGame.onClick.AddListener(() => OnClickExitOnlineGame());
        Button_CreateGame.onClick.AddListener(() => OnClickCreateGame());
        Button_AddGame.onClick.AddListener(() => OnClickAddGame());

        Image_OnlineGame.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnClickEndGame()
    {
        Debug.Log("EndGame");
    }
    private void OnClickSinglePlayer()
    {

        Debug.Log("SinglePlayer");
    }
    private void OnClickOnlineGame()
    {
        Image_OnlineGame.SetActive(true);
    }
    private void OnClickSetting()
    {

        Debug.Log("EndGame");
    }
    private void OnClickExitOnlineGame()
    {
        Image_OnlineGame.SetActive(false);
    }

    private void OnClickCreateGame()
    {

    }
    private void OnClickAddGame()
    {

    }
}
