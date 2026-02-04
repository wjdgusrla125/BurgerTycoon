using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.Serialization;
using static Define;
using static UnityEngine.Rendering.DebugUI;

public class MainCounterSystem : SystemBase
{
	public Grill Grill;
	public Counter Counter;
	public List<Table> Tables = new List<Table>();
	public TrashCan TrashCan;
	public Office OfficeWorker;
	public Office OfficePlayer;
	public DrivethruCounter DrivethruCounter;
	public PackingDesk PackingDesk;
	public KioskCounter KioskCounter;
	public GameObject[] DisabledWalls;
	
	public WorkerController[] Jobs = new WorkerController[(int)EMainCounterJob.MaxCount];
	public override bool HasJob
	{
		get
		{
			for (int i = 0; i < (int)EMainCounterJob.MaxCount; i++)
			{
				EMainCounterJob type = (EMainCounterJob)i;
				if (ShouldDoJob(type))
					return true;
			}

			return false;
		}
	}
	
	private Dictionary<EMainCounterJob, Func<bool>> _jobConditions;
	
	private void Awake()
	{
		Counter.Owner = this;
		InitializeJobConditions();
	}

	private void Update()
	{
		foreach (WorkerController worker in Workers)
		{
			if (worker.WorkerJob != null)
				continue;

			IEnumerator job = DoMainCounterWorkerJob(worker);
			worker.DoJob(job);
		}
	}

	#region Worker
	private void InitializeJobConditions()
	{
		_jobConditions = new Dictionary<EMainCounterJob, Func<bool>>
		{
			[EMainCounterJob.MoveBurgerToCounter] = () => 
				Grill != null 
				&& Grill.CurrentWorker == null 
				&& Grill.BurgerCount > 0 
				&& Counter.NeedMoreBurgers,
        
			[EMainCounterJob.CounterCashier] = () => 
				Counter != null 
				&& Counter.CurrentCashierWorker == null 
				&& Counter.NeedCashier 
				&& Counter.FindTableToServeGuests() != null,
        
			[EMainCounterJob.CleanTable] = () => 
				Tables.Any(table => table.TableState == ETableState.Dirty),
        
			[EMainCounterJob.MoveBurgerToPackingDesk] = () => 
				PackingDesk != null 
				&& PackingDesk.CurrentBurgerWorker == null 
				&& Grill != null 
				&& Grill.BurgerCount > 0 
				&& PackingDesk.NeedMoreBurgers,
        
			[EMainCounterJob.PackingBoxing] = () => 
				PackingDesk != null 
				&& PackingDesk.CurrentPackingBoxWorker == null 
				&& PackingDesk.BurgerCount >= PACKING_BOX_MAX_BURGER_COUNT 
				&& DrivethruCounter.NeedMoreBurgerPacks,
        
			[EMainCounterJob.MoveBoxToDrivethruCounter] = () => 
				PackingDesk != null 
				&& PackingDesk.CurrentMoveBoxWorker == null 
				&& PackingDesk.PackingBoxCount > 0 
				&& DrivethruCounter.NeedMoreBurgerPacks,
        
			[EMainCounterJob.DrivethruCashier] = () => 
				DrivethruCounter != null 
				&& DrivethruCounter.CurrentCashierWorker == null 
				&& DrivethruCounter.NeedCashier 
				&& DrivethruCounter.HasWaitingCar 
				&& !DrivethruCounter.NeedMoreBurgerPacks
		};
	}
	
	bool ShouldDoJob(EMainCounterJob jobType)
	{
		WorkerController wc = Jobs[(int)jobType];
		if (wc != null)
			return false;
    
		return _jobConditions.ContainsKey(jobType) && _jobConditions[jobType]();
	}

	IEnumerator DoMainCounterWorkerJob(WorkerController wc)
	{
		while (true)
		{
			yield return new WaitForSeconds(1);

			bool foundJob = false;
			
			if (ShouldDoJob(EMainCounterJob.MoveBurgerToCounter))
			{
				foundJob = true;
				
				Jobs[(int)EMainCounterJob.MoveBurgerToCounter] = wc;
				
				wc.SetDestination(Grill.WorkerPos.position, () =>
				{
					wc.transform.rotation = Grill.WorkerPos.rotation;
				});
				
				yield return new WaitUntil(() => wc.HasArrivedAtDestination);
				
				wc.transform.rotation = Grill.WorkerPos.rotation;
				yield return new WaitForSeconds(3);
				
				wc.SetDestination(Counter.BurgerWorkerPos.position, () =>
				{
					wc.transform.rotation = Counter.BurgerWorkerPos.rotation;
				});
				
				yield return new WaitUntil(() => wc.HasArrivedAtDestination);
				
				wc.transform.rotation = Counter.BurgerWorkerPos.rotation;
				yield return new WaitForSeconds(2);
				
				Jobs[(int)EMainCounterJob.MoveBurgerToCounter] = null;
			}
			
			if (ShouldDoJob(EMainCounterJob.CounterCashier))
			{
				foundJob = true;
				
				Jobs[(int)EMainCounterJob.CounterCashier] = wc;
				
				wc.SetDestination(Counter.CashierWorkerPos.position);
				
				yield return new WaitUntil(() => wc.HasArrivedAtDestination);
				
				wc.transform.rotation = Counter.CashierWorkerPos.rotation;
				yield return new WaitForSeconds(2);
				
				Jobs[(int)EMainCounterJob.CounterCashier] = null;
			}
			
			if (ShouldDoJob(EMainCounterJob.CleanTable))
			{
				Table table = Tables.Where(t => t.TableState == ETableState.Dirty).FirstOrDefault();
				if (table == null)
					continue;

				foundJob = true;
				
				Jobs[(int)EMainCounterJob.CleanTable] = wc;
				
				wc.SetDestination(table.WorkerPos.position, () => 
				{ 
					wc.transform.rotation = table.WorkerPos.rotation; 
				});
				
				yield return new WaitUntil(() => wc.HasArrivedAtDestination);
				
				wc.transform.rotation = table.WorkerPos.rotation;
				yield return new WaitUntil(() => table.TableState != ETableState.Dirty);
				
				wc.SetDestination(TrashCan.WorkerPos.position, () => 
				{ 
					wc.transform.rotation = TrashCan.WorkerPos.rotation; 
				});
				
				wc.transform.rotation = table.WorkerPos.rotation;
				yield return new WaitUntil(() => wc.IsServing == false);
				
				Jobs[(int)EMainCounterJob.CleanTable] = null;
			}

			if (ShouldDoJob(EMainCounterJob.MoveBurgerToPackingDesk))
			{
				foundJob = true;
				
				Jobs[(int)EMainCounterJob.MoveBurgerToPackingDesk] = wc;
				
				wc.SetDestination(Grill.WorkerPos.position, () =>
				{
					wc.transform.rotation = Grill.WorkerPos.rotation;
				});
				
				yield return new WaitUntil(() => wc.HasArrivedAtDestination);
				
				wc.transform.rotation = Grill.WorkerPos.rotation;
				yield return new WaitForSeconds(3);
				
				wc.SetDestination(PackingDesk.BurgerWorkerPos.position, () =>
				{
					wc.transform.rotation = PackingDesk.BurgerWorkerPos.rotation;
				});
				
				yield return new WaitUntil(() => wc.HasArrivedAtDestination);
				
				wc.transform.rotation = PackingDesk.BurgerWorkerPos.rotation;
				yield return new WaitForSeconds(2);
				
				Jobs[(int)EMainCounterJob.MoveBurgerToPackingDesk] = null;
			}
			
			if (ShouldDoJob(EMainCounterJob.PackingBoxing))
			{
				foundJob = true;
				
				Jobs[(int)EMainCounterJob.PackingBoxing] = wc;
				
				wc.SetDestination(PackingDesk.PackingBoxWorkerPos.position);
				
				yield return new WaitUntil(() => wc.HasArrivedAtDestination);
				
				wc.transform.rotation = PackingDesk.PackingBoxWorkerPos.rotation;
				yield return new WaitForSeconds(2);
				
				Jobs[(int)EMainCounterJob.PackingBoxing] = null;
			}
			
			if (ShouldDoJob(EMainCounterJob.MoveBoxToDrivethruCounter))
			{
				foundJob = true;
				
				Jobs[(int)EMainCounterJob.MoveBoxToDrivethruCounter] = wc;
				
				wc.SetDestination(PackingDesk.MoveBoxWorkerPos.position, () =>
				{
					wc.transform.rotation = PackingDesk.MoveBoxWorkerPos.rotation;
				});
				
				yield return new WaitUntil(() => wc.HasArrivedAtDestination);
				
				wc.transform.rotation = PackingDesk.MoveBoxWorkerPos.rotation;
				yield return new WaitForSeconds(3);
				
				wc.SetDestination(DrivethruCounter.BurgerPackWorkerPos.position, () =>
				{
					wc.transform.rotation = DrivethruCounter.BurgerPackWorkerPos.rotation;
				});
				
				yield return new WaitUntil(() => wc.HasArrivedAtDestination);
				
				wc.transform.rotation = DrivethruCounter.BurgerPackWorkerPos.rotation;
				yield return new WaitForSeconds(2);
				
				Jobs[(int)EMainCounterJob.MoveBoxToDrivethruCounter] = null;
			}
			
			if (ShouldDoJob(EMainCounterJob.DrivethruCashier))
			{
				foundJob = true;
				
				Jobs[(int)EMainCounterJob.DrivethruCashier] = wc;
				
				wc.SetDestination(DrivethruCounter.CashierWorkerPos.position);
				
				yield return new WaitUntil(() => wc.HasArrivedAtDestination);
				
				wc.transform.rotation = DrivethruCounter.CashierWorkerPos.rotation;
				yield return new WaitForSeconds(2);
				
				Jobs[(int)EMainCounterJob.DrivethruCashier] = null;
			}
			
			if (foundJob == false)
			{
				RemoveWorker(wc);
			}	
		}
	}

	public bool HasEmptyCleanTable()
	{
		foreach (Table table in Tables)
		{
			if (table.TableState == ETableState.None)
				return true;
		}

		return false;
	}
	#endregion
}