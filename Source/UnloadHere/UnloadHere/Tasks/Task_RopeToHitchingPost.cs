using System.Collections.Generic;
using System.Text;
using Verse;

namespace ChapaGriff_UnloadHere
{
    public class Task_RopeToHitchingPost : Task
    {
        public List<Pawn> ropees;
        public override JobDef JobDef => DefsOf.RopeRoamerToHitchingPost_Extended;
        public override LocalTargetInfo TargetA => ropees[0];
        public override LocalTargetInfo TargetB => building;
        public Task_RopeToHitchingPost() : base() { } //necessary for IExposable to work
        public Task_RopeToHitchingPost(Building hitchingPost, List<Pawn> ropees) : base(hitchingPost)
        {
            this.ropees = ropees;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref ropees, "ropees", LookMode.Reference);
        }
        public override void CollectGarbage()
        {
            base.CollectGarbage();
            for (var i = ropees.Count - 1; i >= 0; i--)
            {
                if (!UnloadingMapComponent.IsCapableOfUnloading(ropees[i]))
                {
                    //Log.Message($"[UnloadHere] {ropees[i].Name} will be deleted from it's list");
                    ropees.RemoveAt(i);
                }
            }
        }
        public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(base.ToString());
			sb.Append(" -- rope there: ");
            foreach (var ropee in ropees)
            {
                sb.Append(ropee.Name);
                sb.Append(", ");
            }
            sb.Remove(sb.Length - 2, 2);
            return sb.ToString();
		}
        public override bool TryConcat(Task task)
        {
            if (task is Task_RopeToHitchingPost task_TT)
			{
				if (task_TT.building != this.building) return false;
				this.ropees.AddRangeUnique(task_TT.ropees);
				return true;
			}
			return false;
        }
    }
}
