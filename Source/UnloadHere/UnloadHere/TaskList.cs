using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace ChapaGriff_UnloadHere
{
	public class TaskList : IExposable
	{
		public int curTaskIndex = -1;
		public List<Task> list = new List<Task>();
		public Task CurTask
		{
			get
			{
				if (curTaskIndex < 0 || curTaskIndex >= list.Count) return null;
				return list[curTaskIndex];
			}
		}
		public TaskState State
		{
			get
			{
				if (curTaskIndex < 0) return TaskState.Unstarted;
				if (curTaskIndex >= list.Count || (curTaskIndex == list.Count - 1 && CurTask.state == TaskState.Completed)) return TaskState.Completed;
				return TaskState.InProgress;
			}
		}

        public int Count => list.Count;
        public TaskList() { }
        public void ExposeData()
		{
			Scribe_Values.Look(ref curTaskIndex, "curTaskIndex");
			Scribe_Collections.Look(ref list, "list", LookMode.Deep);
			if (Scribe.mode == LoadSaveMode.PostLoadInit && list == null) list = new List<Task>();
        }
		public override string ToString()
		{
			var sb = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				sb.Append(i);
				sb.Append(". ");
				sb.Append(list[i].ToString().Replace("\n- ", "\n\t- "));
				sb.Append('\n');
			}
			return sb.ToString();
		}
		public void Execute(Pawn pawn, bool urgently = false)
		{
			Log.Message($"{pawn.Name} will execute the following taskList{(urgently? " urgently" : "")}:\n{this}");
			if (urgently) for (var i = list.Count - 1; i >= 0; i--) pawn.jobs.jobQueue.EnqueueFirst(list[i].MakeJob());
			else foreach (var task in list) pawn.jobs.jobQueue.EnqueueLast(task.MakeJob());
		}
		public void Optimize(Pawn pawn)
		{
			//grouping tasks by types and buildings because we can concatenate only tasks with the same types and buildings
			var grouped = new Dictionary<Type, Dictionary<Building, List<Task>>>();
			Type type;
			foreach (var task in list)
			{
				type = task.GetType();
				if (!grouped.ContainsKey(type)) grouped.Add(type, new Dictionary<Building, List<Task>>());
				if (!grouped[type].ContainsKey(task.building)) grouped[type].Add(task.building, new List<Task>());
				grouped[type][task.building].Add(task);
			}

			//concatenating similar tasks
			foreach (var (_, byBuilding_) in grouped)
			{
				foreach (var (_, tasks) in byBuilding_)
				{
					for (var i = tasks.Count - 1; i >= 1; i--)
					{
						if (tasks[i - 1].TryConcat(tasks[i])) tasks.RemoveAt(i);
					}
				}
			}

			//grouping tasks by buildings and creating a list of buildings
			var byBuildings = new Dictionary<Building, List<Task>>();
			foreach (var (_, byBuilding_) in grouped)
			{
				foreach (var (building_, tasks) in byBuilding_)
				{
					if (!byBuildings.ContainsKey(building_)) byBuildings[building_] = new List<Task>();
					byBuildings[building_].AddRange(tasks);
				}
			}

            //rewriting TaskList (this)
            list.Clear();
			var buildings = new List<Building>(byBuildings.Keys);
			var pos = pawn.Position;
			Building building;
			while (buildings.Any())
			{
				building = GenClosest.ClosestThing_Global_Reachable(pos, pawn.Map, buildings, PathEndMode.Touch, TraverseParms.For(pawn, mode: TraverseMode.PassDoors)) as Building;
				if (building == null) break;
                list.AddRange(byBuildings[building]);
				pos = building.Position;
				buildings.Remove(building);
			}
		}
		public void StartNextTask()
		{
			curTaskIndex++;
			if (CurTask != null)
            {
                CurTask.state = TaskState.InProgress;
				CurTask.CollectGarbage();
            }
        }
		public void CollectGarbage()
		{
			foreach (var task in list) task.CollectGarbage();
		}
		public void Add(Task task) => list.Add(task);
		public void AddRange(IEnumerable<Task> tasks) => list.AddRange(tasks);
        public void AddRange(TaskList taskList) => list.AddRange(taskList.list);
    }
}
