using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSceneUIManager_Test : MonoBehaviour
{
    public static GameSceneUIManager_Test Instance { get; private set; }

    public Button EndTurn;
    public TextMeshProUGUI CountdownTime;

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

    }



    // Start is called before the first frame update
    void Start()
    {
        







    }





    // Update is called once per frame
    void Update()
    {
        




    }







}
