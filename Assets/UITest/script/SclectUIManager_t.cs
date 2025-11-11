using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SclectUIManager_t : MonoBehaviour
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
        Button_SinglePlayer.onClick.AddListener(()=>OnClickSinglePlayer());
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
        SceneStateManager.Instance.bIsSingle = true;
        SceneManager.LoadScene("UITest_game");
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
        if (SceneStateManager.Instance != null)
        {
            // 获取并保存玩家名
            //string playerName = SceneStateManager.Instance.PlayerName;
           

           // SceneStateManager.Instance.PlayerName = playerName;

            // 设置为服务器模式
            SceneStateManager.Instance.SetAsServer(true);

            Debug.Log($"创建房间 - 玩家名: {SceneStateManager.Instance.PlayerName}, 模式: 服务器");
        }
        else
        {
            Debug.LogError("SceneStateManager.Instance 为空!");
        }


        SceneManager.LoadScene("MainGame");
    }
    private void OnClickAddGame()
    {
        if (SceneStateManager.Instance != null)
        {
           
            // 设置为客户端模式
            SceneStateManager.Instance.SetAsServer(false);

            Debug.Log($"加入房间 - 玩家名: {SceneStateManager.Instance.PlayerName}, 模式: 客户端");
        }
        else
        {
            Debug.LogError("SceneStateManager.Instance 为空!");
        }
        SceneManager.LoadScene("MainGame");

    }
}
