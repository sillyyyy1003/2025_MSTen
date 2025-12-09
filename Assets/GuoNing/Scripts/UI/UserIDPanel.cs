using TMPro;
using UnityEngine;

public class UserIDPanel : MonoBehaviour
{
	[SerializeField] TMP_InputField idInput;
	[SerializeField] GameObject panel;

	private void Start()
	{
		
	}

	public void OnAnimationEnd()
	{
		
		// 初始化时根据 SaveLoadManager 判断是否显示
		if (SaveLoadManager.Instance.HasUserID)
		{
			// 设置当前已有的ID（不触发回调）
			idInput.SetTextWithoutNotify(SaveLoadManager.Instance.CurrentData.userID);
			SceneController.Instance.SwitchScene("SelectScene");
			panel.SetActive(false);
		}
		else
		{
			panel.SetActive(true);
		}
	}

	// 玩家按 “确定”
	public void OnConfirmPressed()
	{
		string id = idInput.text.Trim();

		if (string.IsNullOrEmpty(id))
		{
			Debug.LogWarning("ID cannot be empty");
			return;
		}

		SaveLoadManager.Instance.SetUserID(id);

		SceneController.Instance.SwitchScene("SelectScene");
	}
}