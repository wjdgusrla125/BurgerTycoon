using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;


public class Tutorial : MonoBehaviour
{
	[SerializeField]
	private MainCounterSystem _mainCounterSystem;

	private RestaurantData _data;
	private Dictionary<ETutorialState, System.Func<IEnumerator>> _tutorialSteps;

	private ETutorialState _state
	{
		get { return _data.TutorialState; }
		set { _data.TutorialState = value; }
	}

	public void SetInfo(RestaurantData data)
	{
		_data = data;

		if (_state == ETutorialState.None)
			_state = ETutorialState.CreateFirstTable;

		InitializeTutorialSteps();
		StartCoroutine(CoStartTutorial());
	}

	private void InitializeTutorialSteps()
	{
		Counter counter = _mainCounterSystem.Counter;
		Grill grill = _mainCounterSystem.Grill;
		Table firstTable = _mainCounterSystem.Tables[0];
		Table secondTable = _mainCounterSystem.Tables[1];
		Office officeWorker = _mainCounterSystem.OfficeWorker;
		Office officePlayer = _mainCounterSystem.OfficePlayer;
		TrashCan trashCan = _mainCounterSystem.TrashCan;
		GameObject[] disabledWalls = _mainCounterSystem.DisabledWalls;
		DrivethruCounter drivethruCounter = _mainCounterSystem.DrivethruCounter;
		PackingDesk packingDesk = _mainCounterSystem.PackingDesk;
		KioskCounter kioskCounter = _mainCounterSystem.KioskCounter;

		_tutorialSteps = new Dictionary<ETutorialState, System.Func<IEnumerator>>
		{
			{ ETutorialState.CreateFirstTable, () => StepCreateFirstTable(firstTable) },
			{ ETutorialState.CreateBurgerMachine, () => StepCreateBurgerMachine(grill) },
			{ ETutorialState.CreateCounter, () => StepCreateCounter(counter, grill) },
			{ ETutorialState.PickupBurger, () => StepPickupBurger(grill) },
			{ ETutorialState.PutBurgerOnCounter, () => StepPutBurgerOnCounter(counter) },
			{ ETutorialState.SellBurger, () => StepSellBurger(firstTable) },
			{ ETutorialState.CleanTable, () => StepCleanTable(firstTable, trashCan) },
			{ ETutorialState.CreateSecondTable, () => StepCreateSecondTable(secondTable) },
			{ ETutorialState.CreateWorkerOffice, () => StepCreateWorkerOffice(officeWorker, disabledWalls[0]) },
			{ ETutorialState.CreatePlayerOffice, () => StepCreatePlayerOffice(officePlayer, disabledWalls[1]) },
			{ ETutorialState.CreatePackingDesk, () => StepCreatePackingDesk(packingDesk) },
			{ ETutorialState.CreateDriveThruCounter, () => StepCreateDriveThruCounter(drivethruCounter) },
			{ ETutorialState.CreateKiosk, () => StepCreateKiosk(kioskCounter) }
		};
	}

	IEnumerator CoStartTutorial()
	{
		yield return new WaitForEndOfFrame();

		Counter counter = _mainCounterSystem.Counter;
		Grill grill = _mainCounterSystem.Grill;
		Table firstTable = _mainCounterSystem.Tables[0];
		Table secondTable = _mainCounterSystem.Tables[1];
		Office officeWorker = _mainCounterSystem.OfficeWorker;
		Office officePlayer = _mainCounterSystem.OfficePlayer;
		DrivethruCounter drivethruCounter = _mainCounterSystem.DrivethruCounter;
		KioskCounter kioskCounter = _mainCounterSystem.KioskCounter;
		
		while (_state != ETutorialState.Done)
		{
			if (_tutorialSteps.TryGetValue(_state, out var step))
			{
				yield return step();
			}
			else
			{
				Debug.LogError($"Unknown tutorial state: {_state}");
				break;
			}
		}

		GameManager.Instance.GameSceneUI.SetToastMessage("");
	}

	#region Tutorial Steps

	private IEnumerator StepCreateFirstTable(Table table)
	{
		GameManager.Instance.GameSceneUI.SetToastMessage("Create First Table");
		
		table.SetUnlockedState(EUnlockedState.ProcessingConstruction);
		
		yield return new WaitUntil(() => table.IsUnlocked);
		table.SetUnlockedState(EUnlockedState.Unlocked);
		
		GameManager.Instance.GameSceneUI.StarEffectFromWorldSpace(table.transform.position, () =>
		{
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
		
		_state = ETutorialState.CreateBurgerMachine;
	}

	private IEnumerator StepCreateBurgerMachine(Grill grill)
	{
		GameManager.Instance.GameSceneUI.SetToastMessage("Create BurgerMachine");
		grill.SetUnlockedState(EUnlockedState.ProcessingConstruction);
		yield return new WaitUntil(() => grill.IsUnlocked);
		grill.SetUnlockedState(EUnlockedState.Unlocked);
		
		GameManager.Instance.GameSceneUI.StarEffectFromWorldSpace(grill.transform.position, () =>
		{
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
		
		_state = ETutorialState.CreateCounter;
	}

	private IEnumerator StepCreateCounter(Counter counter, Grill grill)
	{
		GameManager.Instance.GameSceneUI.SetToastMessage("Create Counter");
		counter.SetUnlockedState(EUnlockedState.ProcessingConstruction);
		yield return new WaitUntil(() => counter.IsUnlocked);
		counter.SetUnlockedState(EUnlockedState.Unlocked);
		grill.StopSpawnBurger = false;
		
		GameManager.Instance.GameSceneUI.StarEffectFromWorldSpace(counter.transform.position, () =>
		{
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
		
		_state = ETutorialState.PickupBurger;
	}

	private IEnumerator StepPickupBurger(Grill grill)
	{
		GameManager.Instance.GameSceneUI.SetToastMessage("Pickup Burger");
		yield return new WaitUntil(() => grill.CurrentWorker != null);
		
		GameManager.Instance.GameSceneUI.StarEffectFromWorldSpace(grill.transform.position, () =>
		{
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
		
		_state = ETutorialState.PutBurgerOnCounter;
	}

	private IEnumerator StepPutBurgerOnCounter(Counter counter)
	{
		GameManager.Instance.GameSceneUI.SetToastMessage("Put Burger On Counter");
		yield return new WaitUntil(() => counter.CurrentBurgerWorker != null);
		
		GameManager.Instance.GameSceneUI.StarEffectFromWorldSpace(counter.transform.position, () =>
		{
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
		
		_state = ETutorialState.SellBurger;
	}

	private IEnumerator StepSellBurger(Table table)
	{
		GameManager.Instance.GameSceneUI.SetToastMessage("Sell Burger");
		yield return new WaitUntil(() => table.TableState == Define.ETableState.Reserved);
		
		GameManager.Instance.GameSceneUI.StarEffectFromWorldSpace(table.transform.position, () =>
		{
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
		
		_state = ETutorialState.CleanTable;
	}

	private IEnumerator StepCleanTable(Table table, TrashCan trashCan)
	{
		GameManager.Instance.GameSceneUI.SetToastMessage("");
		yield return new WaitUntil(() => table.TableState == Define.ETableState.Dirty);
		GameManager.Instance.GameSceneUI.SetToastMessage("Clean Table");
		yield return new WaitUntil(() => table.TableState != Define.ETableState.Dirty);
		yield return new WaitUntil(() => trashCan.CurrentWorker != null);
		
		GameManager.Instance.GameSceneUI.StarEffectFromWorldSpace(trashCan.transform.position, () =>
		{
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
		
		_state = ETutorialState.CreateSecondTable;
	}

	private IEnumerator StepCreateSecondTable(Table table)
	{
		GameManager.Instance.GameSceneUI.SetToastMessage("Create Second Table");
		table.SetUnlockedState(EUnlockedState.ProcessingConstruction);
		yield return new WaitUntil(() => table.IsUnlocked);
		table.SetUnlockedState(EUnlockedState.Unlocked);
		
		GameManager.Instance.GameSceneUI.StarEffectFromWorldSpace(table.transform.position, () =>
		{
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
		
		_state = ETutorialState.CreateWorkerOffice;
	}

	private IEnumerator StepCreateWorkerOffice(Office office, GameObject wall)
	{
		GameManager.Instance.GameSceneUI.SetToastMessage("Create WorkerOffice");
		office.SetUnlockedState(EUnlockedState.ProcessingConstruction);
		yield return new WaitUntil(() => office.IsUnlocked);
		wall.SetActive(false);
		office.SetUnlockedState(EUnlockedState.Unlocked);
		
		GameManager.Instance.GameSceneUI.StarEffectFromWorldSpace(office.transform.position, () =>
		{
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
		
		_state = ETutorialState.CreatePlayerOffice;
	}

	private IEnumerator StepCreatePlayerOffice(Office office, GameObject wall)
	{
		GameManager.Instance.GameSceneUI.SetToastMessage("Create PlayerOffice");
		office.SetUnlockedState(EUnlockedState.ProcessingConstruction);
		yield return new WaitUntil(() => office.IsUnlocked);
		wall.SetActive(false);
		office.SetUnlockedState(EUnlockedState.Unlocked);
		
		GameManager.Instance.GameSceneUI.StarEffectFromWorldSpace(office.transform.position, () =>
		{
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
		
		_state = ETutorialState.CreatePackingDesk;
	}

	private IEnumerator StepCreatePackingDesk(PackingDesk desk)
	{
		GameManager.Instance.GameSceneUI.SetToastMessage("Create PackingDesk");
		desk.SetUnlockedState(EUnlockedState.ProcessingConstruction);
		yield return new WaitUntil(() => desk.IsUnlocked);
		desk.SetUnlockedState(EUnlockedState.Unlocked);
		
		GameManager.Instance.GameSceneUI.StarEffectFromWorldSpace(desk.transform.position, () =>
		{
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
		
		_state = ETutorialState.CreateDriveThruCounter;
	}

	private IEnumerator StepCreateDriveThruCounter(DrivethruCounter counter)
	{
		GameManager.Instance.GameSceneUI.SetToastMessage("Create DriveThruCounter");
		counter.SetUnlockedState(EUnlockedState.ProcessingConstruction);
		yield return new WaitUntil(() => counter.IsUnlocked);
		counter.SetUnlockedState(EUnlockedState.Unlocked);
		
		GameManager.Instance.GameSceneUI.StarEffectFromWorldSpace(counter.transform.position, () =>
		{
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
		
		_state = ETutorialState.CreateKiosk;
	}

	private IEnumerator StepCreateKiosk(KioskCounter kioskCounter)
	{
		GameManager.Instance.GameSceneUI.SetToastMessage("Create Kiosk");
		kioskCounter.SetUnlockedState(EUnlockedState.ProcessingConstruction);
		yield return new WaitUntil(() => kioskCounter.IsUnlocked);
		kioskCounter.SetUnlockedState(EUnlockedState.Unlocked);
		
		GameManager.Instance.GameSceneUI.StarEffectFromWorldSpace(kioskCounter.transform.position, () =>
		{
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
		
		_state = ETutorialState.Done;
	}

	#endregion
}