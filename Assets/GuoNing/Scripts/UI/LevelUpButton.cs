using GameData.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System;

public class LevelUpButton : MonoBehaviour
{
	public Button button;

	public void SetButton(Action onButtonClick)
    {
		// button 取消所有回调
		button.onClick.RemoveAllListeners();
		// button 订阅新事件
		button.onClick.AddListener(() => onButtonClick());
	}

	
}
