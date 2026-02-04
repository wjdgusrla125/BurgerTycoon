using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public class Counter : UnlockableBase
{
	private BurgerPile _burgerPile;
	private MoneyPile _moneyPile;

	public MainCounterSystem Owner;

	int _spawnMoneyRemaining = 0;
	int _nextOrderBurgerCount = 0;
	
	int burgerPerMoney = 10;
	public int BurgerPerMoney
	{
		get { return burgerPerMoney; }
		set { burgerPerMoney = value; }
	}

	private List<Transform> _queuePoints = new List<Transform>();
	List<GuestController> _queueGuests = new List<GuestController>();

	public List<WorkerController> Workers = new List<WorkerController>();
	public List<Table> Tables => Owner?.Tables;

	private WorkerInteraction _burgerInteraction;
	public WorkerController CurrentBurgerWorker => _burgerInteraction.CurrentWorker;
	public Transform BurgerWorkerPos;
	public int BurgerCount => _burgerPile.ObjectCount;
	public bool NeedMoreBurgers => (_nextOrderBurgerCount > 0 && BurgerCount < _nextOrderBurgerCount);

	private WorkerInteraction _cashierInteraction;
	public WorkerController CurrentCashierWorker => _cashierInteraction.CurrentWorker;
	public Transform CashierWorkerPos;
	public bool NeedCashier => (CurrentCashierWorker == null);

	[SerializeField]
	public Transform GuestSpawnPos;

	void Start()
    {
		_burgerPile = Utils.FindChild<BurgerPile>(gameObject);
		_moneyPile = Utils.FindChild<MoneyPile>(gameObject);
		_queuePoints = Utils.FindChild<Waypoints>(gameObject).GetPoints();
		
		_burgerInteraction = _burgerPile.GetComponent<WorkerInteraction>();
		_burgerInteraction.InteractInterval = 0.1f;
		_burgerInteraction.OnInteraction = OnBurgerInteraction;
		
		_moneyPile.GetComponent<WorkerInteraction>().InteractInterval = 0.02f;
		_moneyPile.GetComponent<WorkerInteraction>().OnInteraction = OnMoneyInteraction;
		
		GameObject machine = Utils.FindChild(gameObject, "Machine");
		_cashierInteraction = machine.GetComponent<WorkerInteraction>();
		_cashierInteraction.InteractInterval = 1;
		_cashierInteraction.OnInteraction = OnGuestInteraction;
	}

	private void OnEnable()
	{
		StartCoroutine(CoSpawnGuest());
		StartCoroutine(CoSpawnMoney());
		GameManager.Instance.AddEventListener(EEventType.UpgradePlayerIncreaseMoney, OnUpgradeIncreaseMoney);
	}

	private void OnDisable()
	{
		StopAllCoroutines();
		GameManager.Instance.RemoveEventListener(EEventType.UpgradePlayerIncreaseMoney, OnUpgradeIncreaseMoney);
	}

	private void Update()
	{
		UpdateGuestQueueAI();
		UpdateGuestOrderAI();
	}

	IEnumerator CoSpawnGuest()
	{
		while (true)
		{
			yield return new WaitForSeconds(Define.GUEST_SPAWN_INTERVAL);

			if (_queueGuests.Count == _queuePoints.Count)
				continue;

			GameObject go = GameManager.Instance.SpawnGuest();
			go.transform.position = GuestSpawnPos.position;

			Transform dest = _queuePoints.Last();

			GuestController guest = go.GetComponent<GuestController>();
			guest.CurrentDestQueueIndex = _queuePoints.Count - 1;
			guest.GuestState = Define.EGuestState.Queuing;
			guest.SetDestination(dest.position, () => 
			{ 
				guest.transform.rotation = dest.rotation;
			}); 			

			_queueGuests.Add(guest);
		}
	}

	IEnumerator CoSpawnMoney()
	{
		while (true)
		{
			yield return new WaitForSeconds(Define.MONEY_SPAWN_INTERVAL);

			if (_spawnMoneyRemaining <= 0)
				continue;

			_spawnMoneyRemaining--;

			_moneyPile.SpawnObject();
		}
	}
	
	void OnUpgradeIncreaseMoney()
	{
		BurgerPerMoney += 1;
	}

	#region GuestAI
	private void UpdateGuestQueueAI()
	{
		// 줄서기 관리.
		for (int i = 0; i < _queueGuests.Count; i++)
		{
			int guestIndex = i;
			GuestController guest = _queueGuests[guestIndex];
			if (guest.HasArrivedAtDestination == false)
				continue;

			// 다음 지점으로 이동.
			if (guest.CurrentDestQueueIndex > guestIndex)
			{
				guest.CurrentDestQueueIndex--;

				Transform dest = _queuePoints[guest.CurrentDestQueueIndex];
				guest.SetDestination(dest.position, () =>
				{
					guest.transform.rotation = dest.rotation;
				});
			}
		}
	}

	private void UpdateGuestOrderAI()
	{
		// 이미 주문이 진행중이라면 리턴.
		if (_nextOrderBurgerCount > 0)
			return;

		// 손님이 없다면 리턴.
		int maxOrderCount = Mathf.Min(Define.GUEST_MAX_ORDER_BURGER_COUNT, _queueGuests.Count);
		if (maxOrderCount == 0)
			return;

		// 이동중인지 확인.
		GuestController guest = _queueGuests[0];
		if (guest.HasArrivedAtDestination == false)
			return;

		// 맨 앞 자리 도착.
		if (guest.CurrentDestQueueIndex != 0)
			return;

		// 주문 진행.
		int orderCount = Random.Range(1, maxOrderCount + 1);
		_nextOrderBurgerCount = orderCount;
		guest.OrderCount = orderCount;
	}
	#endregion

	#region Interaction
	void OnBurgerInteraction(WorkerController wc)
	{
		_burgerPile.TrayToPile(wc.Tray);
	}

	void OnMoneyInteraction(WorkerController wc)
	{
		_moneyPile.DespawnObjectWithJump(wc.transform.position, () =>
		{
			GameManager.Instance.Money += 100;
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
	}

	void OnGuestInteraction(WorkerController wc)
	{
		Table destTable = FindTableToServeGuests();
		if (destTable == null)
			return;

		for (int i = 0; i < _nextOrderBurgerCount; i++)
		{
			GuestController guest = _queueGuests[i];
			guest.SetDestination(destTable.Chairs[i].position);
			guest.GuestState = Define.EGuestState.Serving;
			guest.OrderCount = 0;

			_burgerPile.PileToTray(guest.Tray);
		}
		
		_spawnMoneyRemaining = _nextOrderBurgerCount * BurgerPerMoney;
		
		destTable.Guests = _queueGuests.GetRange(0, _nextOrderBurgerCount);
		destTable.TableState = Define.ETableState.Reserved;
		
		_queueGuests.RemoveRange(0, _nextOrderBurgerCount);
		
		_nextOrderBurgerCount = 0;
		
		SoundManager.Instance.PlaySfx(SoundType.MoneyGet);
	}

	public Table FindTableToServeGuests()
	{
		if (_nextOrderBurgerCount == 0)
			return null;
		
		if (_burgerPile.ObjectCount < _nextOrderBurgerCount)
			return null;
		
		foreach (Table table in Tables)
		{
			if (table.IsUnlocked == false)
				continue;
			if (table.IsOccupied)
				continue;

			if (_nextOrderBurgerCount > table.Chairs.Count)
				continue;

			return table;
		}

		return null;
	}
	#endregion
}