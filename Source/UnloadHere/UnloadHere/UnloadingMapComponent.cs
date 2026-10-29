using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ChapaGriff_UnloadHere
{
	public class UnloadingMapComponent : MapComponent
	{
        public static readonly List<JobDef> DefsOfJobsUsingTaskLists = new List<JobDef>() { UH_DefOf.ChapaGriff_UH_UnloadSelfAtSpot, UH_DefOf.ChapaGriff_UH_UnloadRopeesAtSpot, UH_DefOf.ChapaGriff_UH_RopeToPen_Extended, UH_DefOf.ChapaGriff_UH_RopeRoamerToHitchingPost_Extended };
        public List<Building_CaravanUnloadingSpot> spots = new List<Building_CaravanUnloadingSpot>();
		public List<Building_CaravanUnloadingSpot>[] spotsByPriority = new List<Building_CaravanUnloadingSpot>[6];
		public Dictionary<Pawn, List<Pawn>> groups = new Dictionary<Pawn, List<Pawn>>(); //ropees grouped by ropers
		public Dictionary<Pawn, TaskList> taskLists = new Dictionary<Pawn, TaskList>();
		private List<Pawn> groupsKeys = null;
		private List<List<Pawn>> groupsValues = null;
		private List<Pawn> taskListsKeys = null;
		private List<TaskList> taskListsValues = null;
		private const int garbageCollectionInterval = 300; //ticks
		private List<Pawn> tmpPawnsToDelete = new List<Pawn>();
		public UnloadingMapComponent(Map map) : base(map)
		{
			for (byte i = 0; i <= 5; i++) spotsByPriority[i] = new List<Building_CaravanUnloadingSpot>();
		}
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Log.Message($"[UnloadHere] UnloadingMapComponent initialized for map with id {map.uniqueID}");
        }
        public override void MapComponentTick()
		{
			base.MapComponentTick();
			if ((Find.TickManager.TicksGame + map.uniqueID) % garbageCollectionInterval == 0) CollectGarbage();
		}
		public override void ExposeData()
		{
			CollectGarbage();
			base.ExposeData();
			Scribe_Collections.Look(ref groups, "groups", LookMode.Reference, LookMode.Reference, ref groupsKeys, ref groupsValues);
			Scribe_Collections.Look(ref taskLists, "taskLists", LookMode.Reference, LookMode.Deep, ref taskListsKeys, ref taskListsValues);
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (groups == null) groups = new Dictionary<Pawn, List<Pawn>>();
				if (taskLists == null) taskLists = new Dictionary<Pawn, TaskList>();
			}
		}
		public void CollectGarbage()
		{
			//Log.Message("[UnloadHere] Collecting garbage...");
			tmpPawnsToDelete.Clear();
			foreach (var (pawn, taskList) in taskLists)
			{
				if (!IsCapableOfUnloading(pawn) || !IsUnloadingOrdered(pawn)) tmpPawnsToDelete.Add(pawn);
				else taskList.CollectGarbage();
			}
			foreach (var pawn in tmpPawnsToDelete)
			{
				//Log.Message($"[UnloadHere] {pawn.Name}'s taskList will be deleted");
				taskLists.Remove(pawn);
				if (groups.ContainsKey(pawn)) groups.Remove(pawn);
			}
			foreach (var group in groups.Values)
			{
				for (var i = group.Count - 1; i >= 0; i--)
				{
					if (!IsCapableOfUnloading(group[i]))
					{
						//Log.Message($"[UnloadHere] {group[i].Name} will be deleted from its group");
						group.RemoveAt(i);
					}
				}
			}
			tmpPawnsToDelete.Clear();
		}
		public static bool IsUnloadingOrdered(Pawn pawn)
		{
			return DefsOfJobsUsingTaskLists.Contains(pawn.jobs.curJob?.def) || pawn.jobs.jobQueue.Any((queuedJob) => DefsOfJobsUsingTaskLists.Contains(queuedJob.job.def));
		}
        public static bool IsCapableOfUnloading(Pawn pawn) => pawn != null && !pawn.Dead && !pawn.Downed;
        public bool AnySpotAccepts(Thing thing)
		{
			foreach (var spot in spots)
			{
				if (spot.Accepts(thing)) return true;
			}
			return false;
        }
        public bool AnyReachableSpotAccepts(Thing thing, Pawn pawn)
        {
            foreach (var spot in spots)
            {
                if (spot.Accepts(thing) && pawn.CanReach(spot, PathEndMode.OnCell, Danger.Deadly)) return true;
            }
            return false;
        }
        //i use this duplicate of original Pawn.IsColonyAnimal because, for some reason, it doesn't work for my tester on 1.6.4518
        public static bool IsColonyAnimal(Pawn pawn) => pawn.IsAnimal && pawn.Faction == Faction.OfPlayer;
		public Building_CaravanUnloadingSpot FindBestSpotFor(Thing thing, Pawn pawn)
		{
			Building_CaravanUnloadingSpot bestSpot = null;
			for (byte i = 5; i > 0; i--)
			{
				bestSpot = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, spotsByPriority[i].FindAll((Building_CaravanUnloadingSpot spot) => spot.Accepts(thing)), PathEndMode.Touch, TraverseParms.For(pawn, mode: TraverseMode.PassDoors)) as Building_CaravanUnloadingSpot;
				if (bestSpot != null) break;
			}
			return bestSpot;
		}
		public void Unload(List<Pawn> pawns, Building shuttle = null, bool draftColonists = false, bool urgently = false)
		{
			if (pawns.Any())
			{
				foreach (var pawn in pawns)
				{
					if (pawn.Map != map)
					{
						Log.Error("[UnloadHere] Unloaded pawns must be on the map of this MapComponent");
						return;
					}
				}
				Log.Message($"[UnloadHere] UNLOADING STARTS. Spots: {spots.Count}. Pawns: {pawns.Count}");
				var colonists = new List<Pawn>();
				var ropees = new List<Pawn>();
				var packRopees = new List<Pawn>(); //if an animal is on this list, it also must be on 'ropees' list
				foreach (Pawn pawn in pawns)
				{
					if (IsCapableOfUnloading(pawn) && !pawn.Drafted)
					{
						if (IsColonyAnimal(pawn))
						{
							if (pawn.RaceProps.packAnimal)
							{
								if (AnimalPenUtility.IsRopeManagedAnimalDef(pawn.def))
								{
									ropees.Add(pawn);
									packRopees.Add(pawn);
								}
								else
								{
									GetTaskListForSelfUnloading(pawn).Execute(pawn, urgently: urgently);
								}
							}
							else if (AnimalPenUtility.IsRopeManagedAnimalDef(pawn.def))
							{
								ropees.Add(pawn);
							}
						}
						else if (pawn.IsColonist)
						{
							taskLists[pawn] = new TaskList(); //task list will be filled later
							groups[pawn] = new List<Pawn>();
							colonists.Add(pawn);
						}
						else if (pawn.IsColonyMechPlayerControlled)
                        {
                            GetTaskListForSelfUnloading(pawn).Execute(pawn, urgently: urgently);
                        }
					}
					pawn.inventory.UnloadEverything = true;
				}
				if (!draftColonists && pawns[0].Map.IsPlayerHome && ropees.Any() && colonists.Any())
				{
					foreach (var ropee in ropees)
					{
						if (!ropee.roping.IsRoped)
						{
							groups[colonists.MinBy((Pawn p) => p.Position.DistanceToSquared(ropee.Position))].Add(ropee);
						}
					}
				}
				foreach (var colonist in colonists)
				{
					if (groups.ContainsKey(colonist))
					{
						taskLists[colonist].AddRange(GetTaskListForUnloadingRopees(colonist, groups[colonist]));
						taskLists[colonist].AddRange(GetTaskListForLeadingRopeesToPen(colonist, groups[colonist]));
					}
					taskLists[colonist].AddRange(GetTaskListForSelfUnloading(colonist, save: false));
                    //Log.Message($"[UnloadHere] {colonist.Name}'s final taskList:\n{taskLists[colonist]}");
                    if (urgently && shuttle != null) colonist.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(UH_DefOf.ChapaGriff_UH_UnloadShuttle, shuttle));
                    taskLists[colonist].Execute(colonist, urgently: urgently);
					if (!urgently && shuttle != null) colonist.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(UH_DefOf.ChapaGriff_UH_UnloadShuttle, shuttle));
				}
			}
		}
		//param 'optimize' makes sense only if 'pawnIsExecutor' = true
		public TaskList GetTaskListForSelfUnloading(Pawn pawn, bool pawnIsExecutor = true, bool save = true, bool optimize = true)
		{
			var taskList = new TaskList();
			if (pawn.inventory.innerContainer.Any() && spots.Any())
			{
				var unvisited = new List<Building_CaravanUnloadingSpot>[6];
				for (byte i = 0; i < 6; i++) unvisited[i] = new List<Building_CaravanUnloadingSpot>(spotsByPriority[i]);
				var pos = pawn.Position;
				var inventory = new List<Thing>(pawn.inventory.innerContainer);
				Building_CaravanUnloadingSpot spot;
				bool accepts;
				for (byte i = 5; i > 0; i--)
				{
					while (unvisited[i].Any() && inventory.Any())
					{
						spot = GenClosest.ClosestThing_Global_Reachable(pos, pawn.Map, unvisited[i], PathEndMode.Touch, TraverseParms.For(pawn, mode: TraverseMode.PassDoors)) as Building_CaravanUnloadingSpot;
						if (spot == null) break;
						var dropList = new List<Thing>();
						accepts = false;
						//inventory check
						for (int j = inventory.Count - 1; j >= 0; j--)
						{
							if (spot.Accepts(inventory[j]))
							{
								accepts = true;
								dropList.Add(inventory[j]);
								inventory.RemoveAt(j);
								//we don't break the cycle because we must "simulate" dropping of accepted things
							}
						}
						if (accepts)
						{
							if (pawnIsExecutor) taskList.Add(new Task_UnloadSelfAtSpot(spot, pawn, dropList));
							else taskList.Add(new Task_UnloadRopeesAtSpot(spot, new Dictionary<Pawn, List<Thing>>() { [pawn] = dropList }));
							pos = spot.Position;
						}
						unvisited[i].Remove(spot);
					}
					if (!inventory.Any()) break;
				}
				if (optimize && pawnIsExecutor) taskList.Optimize(pawn);
			}
			if (save) taskLists[pawn] = taskList;
			//Log.Message($"[UnloadHere] {pawn.Name}'s taskList for self-unloading: (pawnIsExecutor: {pawnIsExecutor}, save: {save}, optimize: {optimize})\n{taskList}");
			return taskList;
		}
		public TaskList GetTaskListForUnloadingRopees(Pawn roper, List<Pawn> ropees, bool optimize = true)
		{
			var taskList = new TaskList();
			if (ropees.Any() && spots.Any())
			{
				foreach (var ropee in ropees)
				{
					if (ropee.RaceProps.packAnimal)	taskList.AddRange(GetTaskListForSelfUnloading(ropee, pawnIsExecutor: false, save: false).list);
				}
				if (optimize) taskList.Optimize(roper);
            }
            //Log.Message($"[UnloadHere] {roper.Name}'s taskList for unloading ropees: (optimize: {optimize})\n{taskList}");
            return taskList;
		}
		public TaskList GetTaskListForLeadingRopeesToPen(Pawn roper, List<Pawn> ropees, bool optimize = true)
		{
			var taskList = new TaskList();
			if (ropees.Any())
			{
				CompAnimalPenMarker penComp;
				Building hitchingPost;
				foreach (var ropee in ropees)
				{
					penComp = AnimalPenUtility.GetPenAnimalShouldBeTakenTo(roper, ropee, out _, mode: RopingPriority.Balanced) ?? null;
					if (penComp != null) taskList.Add(new Task_TakeToPen(penComp.parent as Building, penComp, new List<Pawn>() { ropee }));
					else
					{
						hitchingPost = AnimalPenUtility.GetHitchingPostAnimalShouldBeTakenTo(roper, ropee, out _) ?? null;
						if (hitchingPost != null) taskList.Add(new Task_RopeToHitchingPost(hitchingPost, new List<Pawn>() { ropee }));
					}
				}
				if (optimize) taskList.Optimize(roper);
            }
            //Log.Message($"[UnloadHere] {roper.Name}'s taskList for leading ropees to pen: (optimize: {optimize})\n{taskList}");
            return taskList;
		}
		public void RopeGroup(Pawn roper, bool removeRopeeIfReserved = true, Job jobToReserveFor = null)
		{
			if (groups.ContainsKey(roper))
			{
				Pawn ropee;
				for (var i = groups[roper].Count - 1; i >= 0; i--)
				{
					ropee = groups[roper][i];
					if (IsCapableOfUnloading(ropee) && !ropee.roping.IsRoped)
                    {
						if (roper.CanReserve(ropee))
						{
							roper.roping.RopePawn(ropee);
							if (jobToReserveFor != null) roper.Reserve(ropee, jobToReserveFor);
						}
						else if (removeRopeeIfReserved) groups[roper].Remove(ropee);
                    }
                }
			}
		}
	}
}
