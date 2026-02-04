using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System;
using System.Collections;
using static Define;

public class PackingDesk : UnlockableBase
{
	private BurgerPile _burgerPile;
	private BurgerPackPile _burgerPackPile;
	private BurgerPackingBox _currentPackingBox;
	
	//private int _currentBurgerRemaining = 0;
	public int BurgerCount => _burgerPile.ObjectCount;
	public int PackingBoxCount => _burgerPackPile.ObjectCount;
	
	public List<WorkerController> Workers = new List<WorkerController>();
	
	private WorkerInteraction _burgerInteraction;
	public WorkerController CurrentBurgerWorker => _burgerInteraction.CurrentWorker;
	
	private WorkerInteraction _packingBoxInteraction;
	public WorkerController CurrentPackingBoxWorker => _packingBoxInteraction.CurrentWorker;
	
	private WorkerInteraction _moveBoxInteraction;
	public WorkerController CurrentMoveBoxWorker => _moveBoxInteraction.CurrentWorker;
	
	public bool NeedMoreBurgers => (BurgerCount < PACKING_BOX_MAX_BURGER_COUNT);
	public bool IsPackingBox => (_currentPackingBox != null && !_currentPackingBox.IsFull);
	
	public Transform BurgerWorkerPos;
	public Transform PackingBoxWorkerPos;
	public Transform MoveBoxWorkerPos;
	
	[SerializeField]
	private Transform PackingBoxSpawnPos;

	private void Start()
	{
		_burgerPile = Utils.FindChild<BurgerPile>(gameObject);
		_burgerPackPile = Utils.FindChild<BurgerPackPile>(gameObject);
		
		_burgerInteraction = _burgerPile.GetComponent<WorkerInteraction>();
		_burgerInteraction.InteractInterval = 0.1f;
		_burgerInteraction.OnInteraction = OnBurgerInteraction;
		
		_packingBoxInteraction = PackingBoxSpawnPos.GetComponent<WorkerInteraction>();
		_packingBoxInteraction.InteractInterval = 0.5f;
		_packingBoxInteraction.OnInteraction = OnPackingBoxInteraction;
		
		_moveBoxInteraction = _burgerPackPile.GetComponent<WorkerInteraction>();
		_moveBoxInteraction.InteractInterval = 0.5f;
		_moveBoxInteraction.OnInteraction = OnMoveBoxInteraction;
	}

	#region Interactions

	private void OnBurgerInteraction(WorkerController wc)
	{
		_burgerPile.TrayToPile(wc.Tray);
	}
	
	private void OnPackingBoxInteraction(WorkerController wc)
	{
		if (_currentPackingBox == null && _burgerPile.ObjectCount >= PACKING_BOX_MAX_BURGER_COUNT)
		{
			GameObject boxGO = GameManager.Instance.SpawnPackingBox();
			_currentPackingBox = boxGO.GetComponent<BurgerPackingBox>();
			_currentPackingBox.transform.position = PackingBoxSpawnPos.position;
			_currentPackingBox.transform.rotation = Quaternion.identity;
		}

		// 박스가 꽉 차지 않았으면 버거 이동
		if (_currentPackingBox != null && !_currentPackingBox.IsFull && _burgerPile.ObjectCount > 0)
		{
			_burgerPile.PileToPile(_currentPackingBox._burgerPile);
		}

		// 박스가 다 차면 박스 이동 코루틴 실행
		if (_currentPackingBox != null && _currentPackingBox.IsFull)
		{
			StartCoroutine(DelayedMoveBox(_currentPackingBox));
			_currentPackingBox = null;
		}
	}
	
	private IEnumerator DelayedMoveBox(BurgerPackingBox box)
	{
		yield return new WaitForSeconds(0.7f);
		MoveBoxToPackingPile(box);
	}
	
	private void MoveBoxToPackingPile(BurgerPackingBox box)
	{
		Transform boxTransform = box.transform;
		//box.transform.SetParent(_burgerPackPile.transform);

		boxTransform
			.DOJump(_burgerPackPile.transform.position, 1.5f, 1, 0.5f)
			.OnComplete(() =>
			{
				_burgerPackPile.AddToPile(box.gameObject, jump: false);
			});
	}
	
	private void OnMoveBoxInteraction(WorkerController wc)
	{
		if (_burgerPackPile.ObjectCount <= 0)
			return;

		_burgerPackPile.PileToTray(wc.Tray);
	}

	#endregion
}
