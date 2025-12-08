using UnityEngine;
using UnityEngine.UI;

public class BuildingSlot : MonoBehaviour
{
	[SerializeField] private Image ActiveSlot;

	private void Start()
	{
		ActiveSlot.gameObject.SetActive(false);
	}

	public void SetActiveSlot(bool isActive)
	{
		ActiveSlot.gameObject.SetActive(isActive);
	}

	public void CloseSlot()
	{
		gameObject.SetActive(false); // ÍÆ¼ö£¬²» Destroy
	}
}
