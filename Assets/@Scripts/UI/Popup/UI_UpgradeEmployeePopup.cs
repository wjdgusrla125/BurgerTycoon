using UnityEngine;
using UnityEngine.UI;

public class UI_UpgradeEmployeePopup : MonoBehaviour
{
	[SerializeField]
	Button _closeButton;

	[SerializeField]
	UI_UpgradeEmployeePopupItem _speedItem;

	[SerializeField]
	UI_UpgradeEmployeePopupItem _capacityItem;

	[SerializeField]
	UI_UpgradeEmployeePopupItem _hireItem;

    void Start()
    {
		_closeButton.onClick.AddListener(OnClickCloseButton);

		_speedItem.SetInfo(EUpgradeEmployeePopupItemType.Speed, 500);
		_capacityItem.SetInfo(EUpgradeEmployeePopupItemType.Capacity, 750);
		_hireItem.SetInfo(EUpgradeEmployeePopupItemType.Hire, 1000);
	}

	void OnClickCloseButton()
	{
		gameObject.SetActive(false);
	}
}