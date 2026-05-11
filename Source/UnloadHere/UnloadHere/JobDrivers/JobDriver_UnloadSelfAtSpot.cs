using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ChapaGriff_UnloadHere
{
	public class JobDriver_UnloadSelfAtSpot : JobDriver
	{
		public Building_CaravanUnloadingSpot Spot => TargetThingA as Building_CaravanUnloadingSpot;
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
		public Task_UnloadSelfAtSpot Task => TaskList?.CurTask as Task_UnloadSelfAtSpot;
		public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(pawn, job, errorOnFailed: errorOnFailed);
		protected override IEnumerable<Toil> MakeNewToils()
		{
            //i often use delegates intead of direct references to avoid cases when UnloadingMapComponent (unloadingUtility) isn't initialized and we try to indirectly refer to it
            yield return new Toil()
			{
				initAction = delegate { TaskList?.StartNextTask(); }
			};
			yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.OnCell).FailOnDestroyedNullOrForbidden(TargetIndex.A);
			yield return new Toil()
			{
				initAction = delegate { Task?.Drop(); }
			};
		}
	}
}
