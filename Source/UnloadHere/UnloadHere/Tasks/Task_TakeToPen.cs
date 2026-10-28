using RimWorld;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace ChapaGriff_UnloadHere
{
    public class Task_TakeToPen : Task
	{
		public List<Pawn> ropees;
		public CompAnimalPenMarker comp;
        public override JobDef JobDef => UH_DefOf.ChapaGriff_UH_RopeToPen_Extended;
        public override LocalTargetInfo TargetA => ropees[0];
        public override LocalTargetInfo TargetB => building;
		public override LocalTargetInfo TargetC => building;
		public Task_TakeToPen() : base() { } //necessary for IExposable to work
        public Task_TakeToPen(Building penMarker, CompAnimalPenMarker comp, List<Pawn> ropees) : base(penMarker)
		{
			this.ropees = ropees;
			this.comp = comp;
		}
        public override void ExposeData()
        {
            base.ExposeData();
			Scribe_Collections.Look(ref ropees, "ropees", LookMode.Reference);
			if (Scribe.mode == LoadSaveMode.PostLoadInit) comp = building.GetComp<CompAnimalPenMarker>();
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
			sb.Append(comp.RenamableLabel.ToString());
			sb.Append(" -- take there: ");
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
            if (task is Task_TakeToPen task_TT)
			{
				if (task_TT.building != this.building || task_TT.comp != this.comp) return false;
				this.ropees.AddRangeUnique(task_TT.ropees);
				return true;
			}
			return false;
        }
    }
}
