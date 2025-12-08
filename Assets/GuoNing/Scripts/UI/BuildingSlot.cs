using UnityEngine;
using UnityEngine.UI;

public class BuildingSlot : MonoBehaviour
{
	[SerializeField] private Image ActiveSlot;
	private bool isActivated = false;
	public bool IsActivated => isActivated;

	private void Start()
	{
		ActiveSlot.gameObject.SetActive(false);
	}

	public void SetActiveSlot(bool isActive)
	{
		ActiveSlot.gameObject.SetActive(isActive);
	}

	public void ActivateSlot()
	{
		isActivated = true;
		SetActiveSlot(true);
	}

	public void CloseSlot()
	{
		gameObject.SetActive(false); // ÍÆ¼ö£¬²» Destroy
	}
}
