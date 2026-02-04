using System;
using System.Collections;
using UnityEngine;
using static Define;

[RequireComponent(typeof(CharacterController))]
public class WorkerController : StickmanController
{
	protected CharacterController _controller;
	public SystemBase CurrentSystem;

	public Coroutine WorkerJob;
	public void DoJob(IEnumerator job)
	{
		if (WorkerJob != null)
			StopCoroutine(WorkerJob);
		
		WorkerJob = StartCoroutine(job);
	}

	protected override void Awake()
	{
		base.Awake();

		_controller = GetComponent<CharacterController>();
	}

	private void OnEnable()
	{
		GameManager.Instance.AddEventListener(EEventType.UpgradeWorkerSpeed, OnSpeedWorker);
		GameManager.Instance.AddEventListener(EEventType.UpgradeWorkerCapacity, OnCapacityWorker);
	}
	
	private void OnDisable()
	{
		GameManager.Instance.RemoveEventListener(EEventType.UpgradeWorkerSpeed, OnSpeedWorker);
		GameManager.Instance.RemoveEventListener(EEventType.UpgradeWorkerCapacity, OnCapacityWorker);
	}

	private void Start()
	{
		State = EAnimState.Move;
	}

	protected override void Update()
    {
		base.Update();

		if (HasArrivedAtDestination)
		{
			_navMeshAgent.isStopped = true;
			State = EAnimState.Idle;
		}
		else
		{
			State = EAnimState.Move;
			LookAtDestination();
		}
	}
	
	void OnSpeedWorker()
	{
		_moveSpeed += 0.5f;
		_navMeshAgent.speed = _moveSpeed;
	}
	
	void OnCapacityWorker()
	{
		Tray.Capacity += 1;
	}
}
