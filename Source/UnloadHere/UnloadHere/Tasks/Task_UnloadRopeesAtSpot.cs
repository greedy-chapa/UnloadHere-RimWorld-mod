using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ChapaGriff_UnloadHere
{
    public class Task_UnloadRopeesAtSpot : Task
	{
		public new Building_CaravanUnloadingSpot building;
		public Dictionary<Pawn, List<Thing>> dropLists;
		private List<Pawn> dropListsKeys = null;
        private List<List<Thing>> dropListsValues = null;
		public List<Pawn> unprocessedRopees = new List<Pawn>();
		private List<Pawn> tmpRopeesToDelete = new List<Pawn>();
        public override JobDef JobDef => DefsOf.UnloadRopeesAtSpot;
        public override LocalTargetInfo TargetA => unprocessedRopees[0];
        public override LocalTargetInfo TargetB => building;
		public Task_UnloadRopeesAtSpot() : base() { } //necessary for IExposable to work
        public Task_UnloadRopeesAtSpot(Building_CaravanUnloadingSpot spot, Dictionary<Pawn, List<Thing>> dropLists) : base(spot)
		{
			this.building = spot;
			this.dropLists = dropLists;
			foreach (var (ropee, _) in dropLists) unprocessedRopees.Add(ropee);
		}
        public override void ExposeData()
        {
            base.ExposeData();
			Scribe_References.Look(ref building, "buildingCUS");
			Scribe_Collections.Look(ref dropLists, "dropLists", LookMode.Reference, LookMode.Reference, ref dropListsKeys, ref dropListsValues);
			Scribe_Collections.Look(ref unprocessedRopees, "unprocessedRopees", LookMode.Reference);
        }
        public override void CollectGarbage()
        {
            base.CollectGarbage();
			tmpRopeesToDelete.Clear();
			foreach (var (ropee, dropList) in dropLists)
			{
				if (!UnloadingMapComponent.IsCapableOfUnloading(ropee) || !dropList.Any()) tmpRopeesToDelete.Add(ropee);
                else
				{
					for (var i = dropList.Count - 1; i >= 0; i--)
					{
						if (dropList[i] == null || !ropee.inventory.Contains(dropList[i]))
                        {
							//Log.Message($"[UnloadHere] {dropList[i]} will be deleted from {ropee.Name}'s dropList");
                            dropList.RemoveAt(i);
                        }
                    }
				}
			}
			foreach (var ropee in tmpRopeesToDelete)
			{
				//Log.Message($"[UnloadHere] {ropee.Name}'s dropList will be deleted");
				dropLists.Remove(ropee);
				if (unprocessedRopees.Contains(ropee)) unprocessedRopees.Remove(ropee);
            }
            tmpRopeesToDelete.Clear();
        }
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(building.storageGroup?.RenamableLabel ?? base.ToString());
			sb.Append(':');
			foreach (var (ropee, dropList) in dropLists)
			{
				sb.Append("\n- ");
				sb.Append(ropee.Name);
				sb.Append(" drops: ");
				foreach (var thing in dropList)
				{
					sb.Append(thing.Label);
					sb.Append(", ");
				}
				sb.Remove(sb.Length - 2, 2);
			}
			return sb.ToString();
		}
		public void Drop()
		{
			if (state == TaskState.InProgress)
			{
				foreach (var (ropee, _) in dropLists)
				{
					Drop(ropee);
				}
			}
		}
		public void Drop(Pawn ropee)
		{
			if (state == TaskState.InProgress && dropLists.ContainsKey(ropee))
			{
				if (UnloadingMapComponent.IsCapableOfUnloading(ropee))
				{
					foreach (var thing in dropLists[ropee])
					{
						if (thing != null && ropee.inventory.Contains(thing)) ropee.inventory.innerContainer.TryDrop(thing, building.Position, building.Map, ThingPlaceMode.Near, thing.stackCount, out var _);
					}
				}
				unprocessedRopees.Remove(ropee);
				if (!unprocessedRopees.Any()) state = TaskState.Completed;
			}
		}
        public override bool TryConcat(Task task)
        {
			if (task is Task_UnloadRopeesAtSpot task_TT)
			{
				if (task_TT.building != this.building) return false;
				foreach (var (pawn, dropList) in task_TT.dropLists)
				{
					if (this.dropLists.ContainsKey(pawn)) this.dropLists[pawn].AddRange(dropList);
					else this.dropLists[pawn] = dropList;
				}
				this.unprocessedRopees.AddRangeUnique(task_TT.unprocessedRopees);
				return true;
			}
			return false;
        }
    }
}
