using Verse;
using Verse.AI;

namespace ChapaGriff_UnloadHere
{
	public enum TaskState : byte
	{
		Unstarted,
		InProgress,
		Completed
	}

	public abstract class Task : IExposable
	{
		public Building building;
		public TaskState state = TaskState.Unstarted; //must be InProgress to perform any actions
		public abstract JobDef JobDef { get; }
		public virtual LocalTargetInfo TargetA => null;
		public virtual LocalTargetInfo TargetB => null;
		public virtual LocalTargetInfo TargetC => null;
		public Task() { } //necessary for IExposable to work
		public Task(Building building)
		{
			this.building = building;
		}
        public virtual void ExposeData()
        {
			Scribe_References.Look(ref building, "building");
			Scribe_Values.Look(ref state, "state");
        }
		public virtual void CollectGarbage() { }
		public override string ToString() => building.Label;
		//if concatenation is possible, concatenates the given task with this and returns 'true', otherwise returns 'false'
		public abstract bool TryConcat(Task task);
		public virtual Job MakeJob() => JobMaker.MakeJob(JobDef, TargetA, TargetB, TargetC);
    }
}
