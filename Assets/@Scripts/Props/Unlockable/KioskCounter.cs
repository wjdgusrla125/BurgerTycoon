using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using static Define;

public class KioskCounter : UnlockableBase
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
	
	private List<Transform> _queueRightPoints = new List<Transform>();
	private List<Transform> _queueLeftPoints = new List<Transform>();
	
	List<GuestController> _queueRightGuests = new List<GuestController>();
	List<GuestController> _queueLeftGuests = new List<GuestController>();
	
	public List<WorkerController> Workers = new List<WorkerController>();
	
	private WorkerInteraction _burgerInteraction;
	public WorkerController CurrentBurgerWorker => _burgerInteraction.CurrentWorker;
	public Transform BurgerWorkerPos;
	public int BurgerCount => _burgerPile.ObjectCount;
	public bool NeedMoreBurgers => (_nextOrderBurgerCount > 0 && BurgerCount < _nextOrderBurgerCount);
	
	[SerializeField]
	public Transform GuestSpawnPos;
	
	private bool _isWorkerAdding = false;
	
	void Start()
	{
		_burgerPile = Utils.FindChild<BurgerPile>(gameObject);
		_moneyPile = Utils.FindChild<MoneyPile>(gameObject);
		
		_queueRightPoints = Utils.FindChild<Waypoints>(gameObject, "RightQueue").GetPoints();
		_queueLeftPoints = Utils.FindChild<Waypoints>(gameObject, "LeftQueue").GetPoints();
		
		_burgerInteraction = _burgerPile.GetComponent<WorkerInteraction>();
		_burgerInteraction.InteractInterval = 0.1f;
		_burgerInteraction.OnInteraction = OnBurgerInteraction;
		
		_moneyPile.GetComponent<WorkerInteraction>().InteractInterval = 0.02f;
		_moneyPile.GetComponent<WorkerInteraction>().OnInteraction = OnMoneyInteraction;
	}

	private void OnEnable()
	{
		StartCoroutine(CoSpawnGuest());
		StartCoroutine(CoSpawnMoney());
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}
	
	private void Update()
	{
		UpdateGuestQueueAI();
		UpdateGuestOrderAI();
		UpdateGuestPickupAI();
	}
	
	IEnumerator CoSpawnGuest()
	{
		while (true)
		{
			yield return new WaitForSeconds(Define.GUEST_SPAWN_INTERVAL);

			// 두 줄 중 더 짧은 줄 선택
			bool useRightQueue = _queueRightGuests.Count <= _queueLeftGuests.Count;
			List<GuestController> targetQueue = useRightQueue ? _queueRightGuests : _queueLeftGuests;
			List<Transform> targetPoints = useRightQueue ? _queueRightPoints : _queueLeftPoints;

			if (targetQueue.Count == targetPoints.Count)
				continue;

			GameObject go = GameManager.Instance.SpawnGuest();
			go.transform.position = GuestSpawnPos.position;

			Transform dest = targetPoints.Last();

			GuestController guest = go.GetComponent<GuestController>();
			guest.CurrentDestQueueIndex = targetPoints.Count - 1;
			guest.GuestState = Define.EGuestState.Queuing;
			guest.SetDestination(dest.position, () => 
			{ 
				guest.transform.rotation = dest.rotation;
			});

			targetQueue.Add(guest);
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
	
	#region GuestAI
	private void UpdateGuestQueueAI()
	{
		// 오른쪽 줄 관리
		for (int i = 0; i < _queueRightGuests.Count; i++)
		{
			int guestIndex = i;
			GuestController guest = _queueRightGuests[guestIndex];
			if (guest.HasArrivedAtDestination == false)
				continue;
        
			if (guest.CurrentDestQueueIndex > guestIndex)
			{
				guest.CurrentDestQueueIndex--;

				Vector3 dest = _queueRightPoints[guest.CurrentDestQueueIndex].position;
				guest.Destination = dest;
			}
		}

		// 왼쪽 줄 관리
		for (int i = 0; i < _queueLeftGuests.Count; i++)
		{
			int guestIndex = i;
			GuestController guest = _queueLeftGuests[guestIndex];
			if (guest.HasArrivedAtDestination == false)
				continue;
        
			if (guest.CurrentDestQueueIndex > guestIndex)
			{
				guest.CurrentDestQueueIndex--;

				Vector3 dest = _queueLeftPoints[guest.CurrentDestQueueIndex].position;
				guest.Destination = dest;
			}
		}
	}

	private void UpdateGuestOrderAI()
	{
		if (_nextOrderBurgerCount > 0) return;
    
		// 두 줄을 모두 확인하여 가장 앞에 있는 손님 찾기
		GuestController rightGuest = _queueRightGuests.Count > 0 ? _queueRightGuests[0] : null;
		GuestController leftGuest = _queueLeftGuests.Count > 0 ? _queueLeftGuests[0] : null;
    
		GuestController guest = null;
    
		// 둘 다 있으면 먼저 도착한 손님 선택
		if (rightGuest != null && leftGuest != null)
		{
			guest = rightGuest.HasArrivedAtDestination && rightGuest.CurrentDestQueueIndex == 0 ? rightGuest : null;
			if (guest == null && leftGuest.HasArrivedAtDestination && leftGuest.CurrentDestQueueIndex == 0)
				guest = leftGuest;
		}
		else if (rightGuest != null)
		{
			if (rightGuest.HasArrivedAtDestination && rightGuest.CurrentDestQueueIndex == 0)
				guest = rightGuest;
		}
		else if (leftGuest != null)
		{
			if (leftGuest.HasArrivedAtDestination && leftGuest.CurrentDestQueueIndex == 0)
				guest = leftGuest;
		}
    
		if (guest == null) return;
    
		int totalGuestCount = _queueRightGuests.Count + _queueLeftGuests.Count;
		int maxOrderCount = Mathf.Min(Define.GUEST_MAX_ORDER_BURGER_COUNT, totalGuestCount);
		if (maxOrderCount == 0) return;
    
		int orderCount = Random.Range(1, maxOrderCount + 1);
		_nextOrderBurgerCount = orderCount;
		guest.OrderCount = orderCount;
	}
	
	private void UpdateGuestPickupAI()
	{
	    if (_nextOrderBurgerCount == 0) return;
	    
	    // 워커가 버거를 추가하는 중이면 손님이 버거를 가져갈 수 없음
	    if (_isWorkerAdding) return;

	    if (_burgerPile.ObjectCount < _nextOrderBurgerCount) return;

	    List<GuestController> guestsToRemove = new List<GuestController>();
	    
	    // 오른쪽 줄의 첫 번째 손님 처리
	    if (_queueRightGuests.Count > 0)
	    {
	        GuestController guest = _queueRightGuests[0];
	        if (guest.OrderCount > 0 && guest.HasArrivedAtDestination && guest.CurrentDestQueueIndex == 0)
	        {
	            for (int i = 0; i < guest.OrderCount; i++)
	            {
	                if (_burgerPile.ObjectCount <= 0) break;
	        
	                GameObject burger = _burgerPile.RemoveFromPile();
	            
	                if (burger != null)
	                {
	                    guest.Tray.AddToTray(burger.transform);
	                }
	            }
	            
	            _spawnMoneyRemaining += guest.OrderCount / burgerPerMoney;
	            guest.GuestState = Define.EGuestState.Leaving;
	            guest.OrderCount = 0;
	            guestsToRemove.Add(guest);
	            _queueRightGuests.Remove(guest);
	        }
	    }
	    
	    // 왼쪽 줄의 첫 번째 손님 처리
	    if (_queueLeftGuests.Count > 0)
	    {
	        GuestController guest = _queueLeftGuests[0];
	        if (guest.OrderCount > 0 && guest.HasArrivedAtDestination && guest.CurrentDestQueueIndex == 0)
	        {
	            for (int i = 0; i < guest.OrderCount; i++)
	            {
	                if (_burgerPile.ObjectCount <= 0) break;
	        
	                GameObject burger = _burgerPile.RemoveFromPile();
	            
	                if (burger != null)
	                {
	                    guest.Tray.AddToTray(burger.transform);
	                }
	            }
	            
	            _spawnMoneyRemaining += guest.OrderCount / burgerPerMoney;
	            guest.GuestState = Define.EGuestState.Leaving;
	            guest.OrderCount = 0;
	            guestsToRemove.Add(guest);
	            _queueLeftGuests.Remove(guest);
	        }
	    }
	    
	    foreach (GuestController guest in guestsToRemove)
	    {
	        guest.SetDestination(GuestSpawnPos.position, () =>
	        {
	            GameManager.Instance.DespawnGuest(guest.gameObject);
	            guest.Tray.ClearTray();
	        });
	    }

	    _nextOrderBurgerCount = 0;
	}
	#endregion

	#region Interactions

	void OnBurgerInteraction(WorkerController wc)
	{
		_burgerPile.TrayToPile(wc.Tray);
		_isWorkerAdding = false;
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
