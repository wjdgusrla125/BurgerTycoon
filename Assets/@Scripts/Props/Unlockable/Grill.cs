using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Grill : UnlockableBase
{
	private BurgerPile _burgers;
	private WorkerInteraction _interaction;

	public int BurgerCount => _burgers.ObjectCount;
	public WorkerController CurrentWorker => _interaction.CurrentWorker;
	public Transform WorkerPos;
	public bool StopSpawnBurger = true;

	protected void Awake()
	{
		_burgers = Utils.FindChild<BurgerPile>(gameObject);
		
		_interaction = _burgers.GetComponent<WorkerInteraction>();
		_interaction.InteractInterval = 0.2f;
		_interaction.OnInteraction = OnWorkerBurgerInteraction;
	}

	Coroutine _coSpawnBurger;

	private void OnEnable()
	{
		
		if (_coSpawnBurger != null)
			StopCoroutine(_coSpawnBurger);

		_coSpawnBurger = StartCoroutine(CoSpawnBurgers());
	}

	private void OnDisable()
	{
		if (_coSpawnBurger != null)
			StopCoroutine(_coSpawnBurger);
		_coSpawnBurger = null;
	}

	IEnumerator CoSpawnBurgers()
	{
		while (true)
		{
			yield return new WaitUntil(() => _burgers.ObjectCount < Define.GRILL_MAX_BURGER_COUNT);

			if (StopSpawnBurger == false)
				_burgers.SpawnObject();

			yield return new WaitForSeconds(Define.GRILL_SPAWN_BURGER_INTERVAL);
		}
	}

	void OnWorkerBurgerInteraction(WorkerController pc)
	{
		if (pc.Tray.CurrentTrayObjectType == Define.EObjectType.Trash)
			return;

		_burgers.PileToTray(pc.Tray);
	}
}
