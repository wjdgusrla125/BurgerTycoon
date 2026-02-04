using System;
using UnityEngine;
using static Define;

public class PlayerController : WorkerController
{
	protected override void Awake()
	{
		base.Awake();

		_navMeshAgent.enabled = false;
		Tray.IsPlayer = true;
	}

	private void OnEnable()
	{
		GameManager.Instance.AddEventListener(EEventType.UpgradePlayerSpeed, OnSpeedPlayer);
		GameManager.Instance.AddEventListener(EEventType.UpgradePlayerCapacity, OnCapacityPlayer);
	}
	
	private void OnDisable()
	{
		GameManager.Instance.RemoveEventListener(EEventType.UpgradePlayerSpeed, OnSpeedPlayer);
		GameManager.Instance.RemoveEventListener(EEventType.UpgradePlayerCapacity, OnCapacityPlayer);
	}

	protected override void Update()
	{
		base.Update();

		Vector3 dir = GameManager.Instance.JoystickDir;
		Vector3 moveDir = new Vector3(dir.x, 0, dir.y);
		moveDir = (Quaternion.Euler(0, 45, 0) * moveDir).normalized;

		if (moveDir != Vector3.zero)
		{
			_controller.Move(moveDir * (Time.deltaTime * _moveSpeed));
			
			Quaternion lookRotation = Quaternion.LookRotation(moveDir);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * _rotateSpeed);

			State = EAnimState.Move;
			//SoundManager.Instance.PlaySfx(SoundType.Footstep);
		}
		else
		{
			State = EAnimState.Idle;
		}
		
		transform.position = new Vector3(transform.position.x, 0, transform.position.z);
	}
	
	void OnSpeedPlayer()
	{
		_moveSpeed += 0.5f;
		_navMeshAgent.speed = _moveSpeed;
	}
	
	void OnCapacityPlayer()
	{
		Tray.Capacity += 1;
	}
}