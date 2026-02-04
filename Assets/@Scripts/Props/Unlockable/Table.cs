using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public class Table : UnlockableBase
{
	public List<Transform> Chairs = new List<Transform>();

	public List<GuestController> Guests = new List<GuestController>();

	private TrashPile _trashPile;
	private MoneyPile _moneyPile; 
	private BurgerPile _burgerPile;

	public Transform WorkerPos;

	public int SpawnMoneyRemaining = 0;
	public int SpawnTrashRemaining = 0;
	private float _eatingTimeRemaining = 0;

	ETableState _tableState = ETableState.None;
	public ETableState TableState
	{
		get { return _tableState; }
		set
		{
			_tableState = value;
		}
	}

	public bool IsOccupied
	{
		get 
		{
			if (_trashPile.ObjectCount > 0)
				return true;

			return TableState != ETableState.None;
		}
	}

	private void Start()
	{
		_trashPile = Utils.FindChild<TrashPile>(gameObject);
		_moneyPile = Utils.FindChild<MoneyPile>(gameObject);
		_burgerPile = Utils.FindChild<BurgerPile>(gameObject);
		
		_trashPile.GetComponent<WorkerInteraction>().InteractInterval = 0.02f;
		_trashPile.GetComponent<WorkerInteraction>().OnInteraction = OnTrashInteraction;
		
		_moneyPile.GetComponent<WorkerInteraction>().InteractInterval = 0.02f;
		_moneyPile.GetComponent<WorkerInteraction>().OnInteraction = OnMoneyInteraction;
	}

	private void OnEnable()
	{
		StartCoroutine(CoSpawnTrash());
		StartCoroutine(CoSpawnMoney());
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}

	private void Update()
	{
		UpdateGuestAndTableAI();
	}
	
	private void UpdateGuestAndTableAI()
	{
		if (TableState == ETableState.Reserved)
		{
			foreach (GuestController guest in Guests)
			{
				if (guest.HasArrivedAtDestination == false)
					return;
			}
			
			for (int i = 0; i < Guests.Count; i++)
			{
				GuestController guest = Guests[i];
				guest.GuestState = EGuestState.Eating;
				guest.transform.rotation = Chairs[i].rotation;

				_burgerPile.TrayToPile(guest.Tray);
			}

			_eatingTimeRemaining = Random.Range(5, 11);
			TableState = ETableState.Eating;
		}
		else if (TableState == ETableState.Eating)
		{
			_eatingTimeRemaining -= Time.deltaTime;
			if (_eatingTimeRemaining > 0)
				return;

			_eatingTimeRemaining = 0;
			
			for (int i = 0; i < Guests.Count; i++)
				_burgerPile.DespawnObject();
			
			SpawnTrashRemaining = Guests.Count;
			SpawnMoneyRemaining = Guests.Count;
			
			foreach (GuestController guest in Guests)
			{
				guest.GuestState = EGuestState.Leaving;
				guest.SetDestination(GUEST_LEAVE_POS, () =>
				{
					GameManager.Instance.DespawnGuest(guest.gameObject);
				});
			}
			
			Guests.Clear();
			TableState = ETableState.Dirty;
		}
		else if (TableState == ETableState.Dirty)
		{
			if (SpawnTrashRemaining == 0 && _trashPile.ObjectCount == 0)
				TableState = ETableState.None;
		}
	}

	IEnumerator CoSpawnTrash()
	{
		while (true)
		{
			yield return new WaitForSeconds(Define.TRASH_SPAWN_INTERVAL);

			if (SpawnTrashRemaining <= 0)
				continue;

			SpawnTrashRemaining--;

			_trashPile.SpawnObject();
		}
	}

	IEnumerator CoSpawnMoney()
	{
		while (true)
		{
			yield return new WaitForSeconds(Define.MONEY_SPAWN_INTERVAL);

			if (SpawnMoneyRemaining <= 0)
				continue;

			SpawnMoneyRemaining--;

			_moneyPile.SpawnObject();
		}
	}

	#region Interaction
	void OnTrashInteraction(WorkerController wc)
	{
		if (wc.Tray.CurrentTrayObjectType == Define.EObjectType.Burger)
			return;

		_trashPile.PileToTray(wc.Tray);
	}

	void OnMoneyInteraction(WorkerController wc)
	{
		_moneyPile.DespawnObjectWithJump(wc.transform.position, () =>
		{
			GameManager.Instance.Money += 100;
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
	}
	#endregion
}
