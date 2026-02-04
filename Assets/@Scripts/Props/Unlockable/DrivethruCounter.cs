using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;
using Random = UnityEngine.Random;

public class DrivethruCounter : UnlockableBase
{
	private BurgerPackPile _burgerPackPile;
	private MoneyPile _moneyPile;
	
	public MainCounterSystem Owner;
	
	int _spawnMoneyRemaining = 0;
	int _nextOrderBurgerPackCount = 0;
	
	int burgerPacksPerMoney = 5;
	public int BurgerPacksPerMoney
	{
		get { return burgerPacksPerMoney; }
		set { burgerPacksPerMoney = value; }
	}
	
	private List<Transform> _queuePoints = new List<Transform>();
	List<CarController> _queueCars = new List<CarController>();
	
	public List<WorkerController> Workers = new List<WorkerController>();
	
	private WorkerInteraction _burgerPackInteraction;
	public WorkerController CurrentBurgerPackWorker => _burgerPackInteraction.CurrentWorker;
	public Transform BurgerPackWorkerPos;
	public int BurgerPackCount => _burgerPackPile.ObjectCount;
	public bool NeedMoreBurgerPacks => (_nextOrderBurgerPackCount > 0 && BurgerPackCount < _nextOrderBurgerPackCount);
	
	private WorkerInteraction _cashierInteraction;
	public WorkerController CurrentCashierWorker => _cashierInteraction.CurrentWorker;
	public Transform CashierWorkerPos;
	public bool NeedCashier => (CurrentCashierWorker == null && _nextOrderBurgerPackCount > 0);
	
	public bool HasWaitingCar => _nextOrderBurgerPackCount > 0;
	public bool IsSellBurgerBox = false;
	
	[SerializeField]
	public Transform CarSpawnPos;

	private void Start()
	{
		_burgerPackPile = Utils.FindChild<BurgerPackPile>(gameObject);
		_moneyPile = Utils.FindChild<MoneyPile>(gameObject);
		_queuePoints = Utils.FindChild<Waypoints>(gameObject).GetPoints();
		
		_burgerPackInteraction = _burgerPackPile.GetComponent<WorkerInteraction>();
		_burgerPackInteraction.InteractInterval = 0.1f;
		_burgerPackInteraction.OnInteraction = OnBurgerPackInteraction;
		
		_moneyPile.GetComponent<WorkerInteraction>().InteractInterval = 0.02f;
		_moneyPile.GetComponent<WorkerInteraction>().OnInteraction = OnMoneyInteraction;
		
		GameObject machine = Utils.FindChild(gameObject, "Machine");
		_cashierInteraction = machine.GetComponent<WorkerInteraction>();
		_cashierInteraction.InteractInterval = 1;
		_cashierInteraction.OnInteraction = OnCarInteraction;
	}

	private void OnEnable()
	{
		StartCoroutine(CoSpawnCar());
		StartCoroutine(CoSpawnMoney());
	}
	
	private void OnDisable()
	{
		StopAllCoroutines();
	}
	
	private void Update()
	{
		UpdateCarQueueAI();
		UpdateCarOrderAI();
	}
	
	IEnumerator CoSpawnCar()
	{
		while (true)
		{
			yield return new WaitForSeconds(CAR_SPAWN_INTERVAL);

			if (_queueCars.Count == _queuePoints.Count) continue;

			GameObject go = GameManager.Instance.SpawnCar();
			go.transform.position = CarSpawnPos.position;

			CarController car = go.GetComponent<CarController>();
			int queuePosition = _queueCars.Count;
			
			car.CurrentDestQueueIndex = queuePosition;
			car.CarState = ECarState.Queuing;
			car.Destination = _queuePoints[queuePosition].position;

			_queueCars.Add(car);
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
	
	#region CarAI
	private void UpdateCarQueueAI()
	{
		for (int i = 0; i < _queueCars.Count; i++)
		{
			int carIndex = i;
			CarController car = _queueCars[carIndex];
			if (car.HasArrivedAtDestination == false)
				continue;

			if (car.CurrentDestQueueIndex > carIndex)
			{
				car.CurrentDestQueueIndex--;

				Vector3 dest = _queuePoints[car.CurrentDestQueueIndex].position;
				car.Destination = dest;
			}
		}
	}

	private void UpdateCarOrderAI()
	{
		if (_nextOrderBurgerPackCount > 0) return;
		
		int maxOrderCount = Mathf.Min(Define.GUEST_MAX_ORDER_BURGER_COUNT, _queueCars.Count);
		if (maxOrderCount == 0) return;
		
		CarController car = _queueCars[0];
		if (car.HasArrivedAtDestination == false) return;
		
		if (car.CurrentDestQueueIndex != 0) return;
		
		int orderCount = Random.Range(1, maxOrderCount + 1);
		_nextOrderBurgerPackCount = orderCount;
		car.OrderCount = orderCount;
		
		SoundManager.Instance.PlaySfx(SoundType.Car);
	}
	#endregion
	
	#region Interaction
	void OnBurgerPackInteraction(WorkerController wc)
	{
		_burgerPackPile.TrayToPile(wc.Tray);
	}

	void OnMoneyInteraction(WorkerController wc)
	{
		_moneyPile.DespawnObjectWithJump(wc.transform.position, () =>
		{
			GameManager.Instance.Money += 100;
			GameManager.Instance.AddExp(EXP_AMOUNT);
		});
	}

	void OnCarInteraction(WorkerController wc)
	{
		if (_nextOrderBurgerPackCount == 0)
			return;

		CarController car = _queueCars[0];


		int availableBurgerCount = _burgerPackPile.ObjectCount;
		if (availableBurgerCount < _nextOrderBurgerPackCount)
			return;

		for (int i = 0; i < _nextOrderBurgerPackCount; i++)
		{
			_burgerPackPile.PileToTray(car.Tray);
		}

		_spawnMoneyRemaining = _nextOrderBurgerPackCount * 10;

		car.SetDestination(CAR_LEAVE_POS, () =>
		{
			GameManager.Instance.DespawnCar(car.gameObject);
			car.Tray.ClearTray();
		});
		car.CarState = ECarState.Leaving;
		car.OrderCount = 0;
		_queueCars.Remove(car);
		_nextOrderBurgerPackCount = 0;

		IsSellBurgerBox = true;
	}
	#endregion
}