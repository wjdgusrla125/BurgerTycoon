using UnityEngine;
using static Define;

public enum EOfficeType
{
	None,
	WorkerUpgrade,
	PlayerUpgrade
}

[RequireComponent(typeof(WorkerInteraction))]
public class Office : UnlockableBase
{
	public EOfficeType OfficeType = EOfficeType.None;
	
	private void Start()
	{
		GetComponent<WorkerInteraction>().OnTriggerStart = OnEnterOffice;
		GetComponent<WorkerInteraction>().OnTriggerEnd = OnLeaveOffice;		
	}

	public void OnEnterOffice(WorkerController wc)
	{
		switch (OfficeType)
		{
			case EOfficeType.WorkerUpgrade:
				GameManager.Instance.UpgradeEmployeePopup.gameObject.SetActive(true);
				break;
			case EOfficeType.PlayerUpgrade:
				GameManager.Instance.UpgradePlayerPopup.gameObject.SetActive(true);
				break;
		}
		
	}

	public void OnLeaveOffice(WorkerController wc)
	{
		switch (OfficeType)
		{
			case EOfficeType.WorkerUpgrade:
				GameManager.Instance.UpgradePlayerPopup.gameObject.SetActive(false);
				break;
			case EOfficeType.PlayerUpgrade:
				GameManager.Instance.UpgradePlayerPopup.gameObject.SetActive(false);
				break;
		}
	}
}