using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ChapaGriff_UnloadHere
{
	public class JobDriver_RopeToPen_Extended: JobDriver_RopeToPen
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
        public TaskList TaskList
		{
			get
			{
				if (UnloadingMC != null && UnloadingMC.taskLists.ContainsKey(pawn)) return UnloadingMC.taskLists[pawn];
				return null;
			}
		}
		public Task_TakeToPen Task => TaskList?.CurTask as Task_TakeToPen;
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
        protected override void ProcessArrivedRopee(Pawn ropee)
		{
			if (Task?.state == TaskState.InProgress && Task.ropees.Contains(ropee))
			{
				base.ProcessArrivedRopee(ropee);
				UnloadingMC.groups[pawn].Remove(ropee);
				Task.ropees.Remove(ropee);
				if (!Task.ropees.Any()) Task.state = TaskState.Completed;
			}
        }
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
