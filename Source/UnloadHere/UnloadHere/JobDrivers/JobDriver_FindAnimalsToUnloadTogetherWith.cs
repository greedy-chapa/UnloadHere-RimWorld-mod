using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ChapaGriff_UnloadHere
{
	public class JobDriver_FindAnimalsToUnloadTogetherWith : JobDriver_RopeToDestination
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
		protected override bool HasRopeeArrived(Pawn ropee, bool roperWaitingAtDest) => pawn.Position.InHorDistOf(TargetThingB.Position, 1.5f);
		protected override void ProcessArrivedRopee(Pawn ropee) { }
		protected override bool ShouldOpportunisticallyRopeAnimal(Pawn animal) => UnloadingMapComponent.IsColonyAnimal(animal) && animal.RaceProps.packAnimal &&
			AnimalPenUtility.IsRopeManagedAnimalDef(animal.def) && animal.Spawned && !animal.Dead && !animal.Downed && animal.MentalStateDef == null &&
			!animal.roping.HasAnyRope && pawn.CanReserve(animal) && animal.inventory.innerContainer.Any((Thing item) => UnloadingMC.AnyReachableSpotAccepts(item, pawn));
		protected override IEnumerable<Toil> MakeNewToils()
		{
			var baseToils = base.MakeNewToils().ToList();
			//we return only first 8 toils because they're accountable for roping animals
			for (var i = 0; i < 8; i++) yield return baseToils[i];
			yield return new Toil()
			{
				initAction = delegate
				{
					UnloadingMC.groups[pawn] = new List<Pawn>(pawn.roping.Ropees);
					var taskList = UnloadingMC.GetTaskListForUnloadingRopees(pawn, pawn.roping.Ropees);
					taskList.AddRange(UnloadingMC.GetTaskListForLeadingRopeesToPen(pawn, pawn.roping.Ropees).list);
					UnloadingMC.taskLists[pawn] = taskList;
					taskList.Execute(pawn, urgently: true);
				}
			};
		}
	}
}
