using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public enum EUpgradeEmployeePopupItemType
{
	None,
	Speed,
	Capacity,
	Hire
}

public class UI_UpgradeEmployeePopupItem : MonoBehaviour
{
	[SerializeField]
	private Button _purchaseButton;

	[SerializeField]
	private TextMeshProUGUI _costText;

	EUpgradeEmployeePopupItemType _type = EUpgradeEmployeePopupItemType.None;

	long _money = 0;
	
    void Start()
    {
		_purchaseButton.onClick.AddListener(OnClickPurchaseButton);
    }

	public void SetInfo(EUpgradeEmployeePopupItemType type, long money)
	{
		_type = type;
		_money = money;
		RefreshUI();
	}

	public void RefreshUI()
	{
		_costText.text = Utils.GetMoneyText(_money);
	}

	public void OnClickPurchaseButton()
	{
		if (GameManager.Instance.Money < _money) return;
	
		GameManager.Instance.Money -= _money;

		switch (_type)
		{
			case EUpgradeEmployeePopupItemType.Speed:
				{
					GameManager.Instance.BroadcastEvent(EEventType.UpgradeWorkerSpeed);
					_money = (long)(_money * 1.5f);
					RefreshUI();
				}
				break;
			case EUpgradeEmployeePopupItemType.Capacity:
				{
					GameManager.Instance.BroadcastEvent(EEventType.UpgradeWorkerCapacity);
					_money = (long)(_money * 1.5f);
					RefreshUI();
				}
				break;
			case EUpgradeEmployeePopupItemType.Hire:
				{
					GameManager.Instance.BroadcastEvent(EEventType.HireWorker);
					GameManager.Instance.UpgradeEmployeePopup.gameObject.SetActive(false);
				}
				break;
		}
	}
}