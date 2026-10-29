using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ChapaGriff_UnloadHere
{
	public class JobDriver_UnloadShuttle : JobDriver
	{
		public Thing ThShuttle => TargetThingA;
		private UnloadingMapComponent unloadingMC;
		public UnloadingMapComponent UnloadingMC
		{
			get
			{
				if (unloadingMC == null) unloadingMC = Map.GetComponent<UnloadingMapComponent>();
				return unloadingMC;
			}
		}
		public List<ThingCount> takeList = new List<ThingCount>();
		public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(pawn, job, errorOnFailed: errorOnFailed);
		protected override IEnumerable<Toil> MakeNewToils()
		{
			if (ModsConfig.IsActive("ludeon.rimworld.odyssey"))
			{
				foreach (var option in MakeNewToilsForShuttle()) yield return option;
			}
		}
		private IEnumerable<Toil> MakeNewToilsForShuttle()
		{
			var Shuttle = ThShuttle as Building_PassengerShuttle;
			yield return new Toil()
			{
				initAction = delegate {
					var spots = new List<Thing>();
					var thingsBySpots = new Dictionary<Building_CaravanUnloadingSpot, List<Thing>>();
					Building_CaravanUnloadingSpot spot;
					foreach (var thing in Shuttle.TransporterComp.innerContainer)
					{
						if (!pawn.Map.reservationManager.IsReserved(thing))
						{
							spot = UnloadingMC.FindBestSpotFor(thing, pawn);
							if (spot != null)
							{
								if (!spots.Contains(spot))
								{
									spots.Add(spot);
									thingsBySpots.Add(spot, new List<Thing>());
								}
								thingsBySpots[spot].Add(thing);
							}
						}
					}
					//i do the main check for ending the job here because all the things are already checked
					//and in the case the job was started without pre-check
					if (!spots.Any()) EndJobWith(JobCondition.Succeeded);
					var freeSpace = MassUtility.FreeSpace(pawn);
					ThingCount thingCount = new ThingCount();
					var list = new List<ThingCount>(1) { thingCount };
					float capacity;
					Thing sepThing;
					while (spots.Count > 0)
					{
						spot = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, spots, PathEndMode.Touch, TraverseParms.For(pawn, mode: TraverseMode.PassDoors)) as Building_CaravanUnloadingSpot;
						foreach (var thing in thingsBySpots[spot])
						{
							for (var count = thing.stackCount; count > 0; count--)
							{
								thingCount = new ThingCount(thing, count);
								list[0] = thingCount;
								capacity = CollectionsMassCalculator.MassUsage(list, IgnorePawnsInventoryMode.Ignore);
								if (capacity <= freeSpace)
								{
									if (count == thing.stackCount) pawn.Reserve(thing, job);
									else
									{
										sepThing = thing.SplitOff(count);
										thingCount = new ThingCount(sepThing, count);
										Shuttle.TransporterComp.innerContainer.TryAdd(sepThing, false);
										pawn.Reserve(sepThing, job);
									}
									takeList.Add(thingCount);
									freeSpace -= capacity;
									break;
								}
							}
							if (freeSpace == 0) break;
						}
						spots.Remove(spot);
						if (freeSpace == 0) break;
					}
				}
			};
			yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.InteractionCell).FailOnDestroyedNullOrForbidden(TargetIndex.A);
			yield return new Toil()
			{
				initAction = delegate
				{
					foreach (var thingCount in takeList)
					{
						Shuttle.TransporterComp.innerContainer.TryTransferToContainer(thingCount.Thing, pawn.inventory.innerContainer, thingCount.Count);
					}
                    //here i do only a lightweight check because the main check will be done in the next UnloadShuttle job
					//but anyway i do it for player not to see a redundant job in the queue
                    if (Shuttle.TransporterComp.innerContainer.Any) pawn.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(UH_DefOf.ChapaGriff_UH_UnloadShuttle, Shuttle));
					UnloadingMC.GetTaskListForSelfUnloading(pawn).Execute(pawn, urgently: true);
				}
			};
		}

    }
}
