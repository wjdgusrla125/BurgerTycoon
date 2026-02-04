using UnityEngine;
using UnityEngine.UI;

public class UI_UpgradePlayerPopup : MonoBehaviour
{
	[SerializeField]
	Button _closeButton;

	[SerializeField]
	UI_UpgradePlayerPopupItem _speedItem;

	[SerializeField]
	UI_UpgradePlayerPopupItem _capacityItem;

	[SerializeField]
	UI_UpgradePlayerPopupItem _increaseItem;
	
	void Start()
	{
		_closeButton.onClick.AddListener(OnClickCloseButton);

		_speedItem.SetInfo(EUpgradePlayerPopupItemType.Speed, 500);
		_capacityItem.SetInfo(EUpgradePlayerPopupItemType.Capacity, 800);
		_increaseItem.SetInfo(EUpgradePlayerPopupItemType.IncreaseMoney, 1000);
	}

	void OnClickCloseButton()
	{
		gameObject.SetActive(false);
	}
}