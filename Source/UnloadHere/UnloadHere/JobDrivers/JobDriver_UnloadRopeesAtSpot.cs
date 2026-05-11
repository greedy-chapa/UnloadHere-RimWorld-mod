using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ChapaGriff_UnloadHere
{
	public class JobDriver_UnloadRopeesAtSpot : JobDriver_RopeToDestination
	{
		private UnloadingMapComponent unloadingMC;
		public UnloadingMapComponent UnloadingMC
		{
			get
			{
				if (unloadingMC == null) unloadingMC = Map.GetComponent<UnloadingMapComponent>();
				return unloadingMC;
			}
		}
		private Building_CaravanUnloadingSpot Spot => TargetThingB as Building_CaravanUnloadingSpot;
		public TaskList TaskList
		{
			get
			{
				if (UnloadingMC != null && UnloadingMC.taskLists.ContainsKey(pawn)) return UnloadingMC.taskLists[pawn];
				return null;
			}
		}
		public Task_UnloadRopeesAtSpot Task => TaskList?.CurTask as Task_UnloadRopeesAtSpot;
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			var group = UnloadingMC.groups[pawn];
            if (pawn.CanReserve(TargetA) && group.Contains(TargetPawnA)) return pawn.Reserve(TargetA, job, errorOnFailed: errorOnFailed);
			else
			{
				group.Remove(TargetPawnA);
				Pawn ropee;
				for (var i = group.Count - 1; i >= 0; i--)
				{
					ropee = group[i];
					if (pawn.CanReserve(ropee) && group.Contains(ropee))
					{
						job.SetTarget(TargetIndex.A, ropee);
						return pawn.Reserve(ropee, job, errorOnFailed: errorOnFailed);
					}
					else group.RemoveAt(i);
                }
			}
			return false;
		}
		protected override bool HasRopeeArrived(Pawn ropee, bool roperWaitingAtDest) => pawn.Position.InHorDistOf(Spot.Position, 1.5f);
		protected override void ProcessArrivedRopee(Pawn ropee) => Task?.Drop(ropee);
		protected override bool ShouldOpportunisticallyRopeAnimal(Pawn animal) => false;
		//i don't know what this method does but if it return 'true', my code works, so it returns 'true'
		protected override bool UpdateDestination() => true;
		protected override IEnumerable<Toil> MakeNewToils()
		{
			yield return new Toil()
			{
				initAction = delegate //why i use delegate i'll explain in JobDriver_UnloadSelfAtSpot.cs
				{
					UnloadingMC.RopeGroup(pawn, jobToReserveFor: job);
					TaskList?.StartNextTask();
				}
			};
			foreach (var toil in base.MakeNewToils()) yield return toil;
		}
	}
}
