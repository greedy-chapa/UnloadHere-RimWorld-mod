using System.Collections.Generic;
using System.Text;
using Verse;

namespace ChapaGriff_UnloadHere
{
	public class Task_UnloadSelfAtSpot : Task
	{
		public new Building_CaravanUnloadingSpot building;
		public Pawn pawn;
		public List<Thing> dropList = null;
		public bool dropEverything = false; //if true, 'dropList' makes no sense
		public override JobDef JobDef => DefsOf.UnloadSelfAtSpot;
		public override LocalTargetInfo TargetA => building;
		public Task_UnloadSelfAtSpot() : base() { } //necessary for IExposable to work
		public Task_UnloadSelfAtSpot(Building_CaravanUnloadingSpot spot, Pawn pawn, bool dropEverything = true) : base(spot)
		{
			this.building = spot;
			this.pawn = pawn;
			this.dropEverything = dropEverything;
		}
		public Task_UnloadSelfAtSpot(Building_CaravanUnloadingSpot spot, Pawn pawn, List<Thing> dropList) : this(spot, pawn, false)
		{
			this.dropList = dropList;
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref building, "buildingCUS");
			Scribe_References.Look(ref pawn, "pawn");
			Scribe_Collections.Look(ref dropList, "dropList", LookMode.Reference);
			Scribe_Values.Look(ref dropEverything, "dropEverything");
		}
		public override void CollectGarbage()
		{
			base.CollectGarbage();
			if (dropList != null)
			{
				for (var i = dropList.Count - 1; i >= 0; i--)
				{
					if (dropList[i] == null || !pawn.inventory.Contains(dropList[i]))
					{
						//Log.Message($"[UnloadHere] {dropList[i]} will be deleted from {pawn.Name}'s dropList");
						dropList.RemoveAt(i);
					}
				}
			}
		}
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(building.storageGroup?.RenamableLabel ?? base.ToString());
			sb.Append(" -- ");
			sb.Append(pawn.Name);
			sb.Append(" drops: ");
			foreach (var thing in dropList)
			{
				sb.Append(thing.Label);
				sb.Append(", ");
			}
			sb.Remove(sb.Length - 2, 2);
			return sb.ToString();
		}
		public void Drop()
		{
			if (state == TaskState.InProgress)
			{
				if (dropEverything)
				{
					Thing thing;
					for (var i = pawn.inventory.innerContainer.Count - 1; i >= 0; i--)
					{
						thing = pawn.inventory.innerContainer[i];
						if (thing != null && building.Accepts(thing)) pawn.inventory.innerContainer.TryDrop(thing, building.Position, building.Map, ThingPlaceMode.Near, thing.stackCount, out var _);
					}
				}
				else
				{
					foreach (var thing in dropList)
					{
						if (thing != null && pawn.inventory.Contains(thing)) pawn.inventory.innerContainer.TryDrop(thing, building.Position, building.Map, ThingPlaceMode.Near, thing.stackCount, out var _);
					}
				}
				state = TaskState.Completed;
			}
		}
		public override bool TryConcat(Task task)
		{
			if (task is Task_UnloadSelfAtSpot task_TT)
			{
				if (task_TT.building != this.building) return false;
				if (task_TT.pawn != this.pawn) return false;
				if (task_TT.dropList != null && this.dropList != null) this.dropList.AddRange(task_TT.dropList);
				if (task_TT.dropEverything) this.dropEverything = true;
				return true;
			}
			return false;
		}
	}
}
