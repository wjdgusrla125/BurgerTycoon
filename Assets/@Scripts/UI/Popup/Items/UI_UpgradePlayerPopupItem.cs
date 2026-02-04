using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public enum EUpgradePlayerPopupItemType
{
	None,
	Speed,
	Capacity,
	IncreaseMoney
}

public class UI_UpgradePlayerPopupItem : MonoBehaviour
{
	[SerializeField]
	private Button _purchaseButton;

	[SerializeField]
	private TextMeshProUGUI _costText;
	
	EUpgradePlayerPopupItemType _type = EUpgradePlayerPopupItemType.None;
	
	long _money = 0;
	
	void Start()
	{
		_purchaseButton.onClick.AddListener(OnClickPurchaseButton);
	}
	
	public void SetInfo(EUpgradePlayerPopupItemType type, long money)
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
			case EUpgradePlayerPopupItemType.Speed:
				{
					GameManager.Instance.BroadcastEvent(EEventType.UpgradePlayerSpeed);
					_money = (long)(_money * 1.5f);
					RefreshUI();
				}
				break;
			case EUpgradePlayerPopupItemType.Capacity:
				{
					GameManager.Instance.BroadcastEvent(EEventType.UpgradePlayerCapacity);
					_money = (long)(_money * 1.5f);
					RefreshUI();
				}
				break;
			case EUpgradePlayerPopupItemType.IncreaseMoney:
				{
					GameManager.Instance.BroadcastEvent(EEventType.UpgradePlayerIncreaseMoney);
					_money = (long)(_money * 2f);
					RefreshUI();
				}
				break;
		}
	}
}
