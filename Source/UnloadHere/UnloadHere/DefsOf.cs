using System.Collections.Generic;
using Verse;

namespace ChapaGriff_UnloadHere
{
	public static class DefsOf
	{
		public static ThingDef CaravanUnloadingSpot => FullDefsOf.ChapaGriff_UH_CaravanUnloadingSpot;
		public static JobDef UnloadSelfAtSpot => FullDefsOf.ChapaGriff_UH_UnloadSelfAtSpot;
		public static JobDef UnloadRopeesAtSpot => FullDefsOf.ChapaGriff_UH_UnloadRopeesAtSpot;
		public static JobDef RopeToPen_Extended => FullDefsOf.ChapaGriff_UH_RopeToPen_Extended;
		public static JobDef RopeRoamerToHitchingPost_Extended => FullDefsOf.ChapaGriff_UH_RopeRoamerToHitchingPost_Extended;
		public static JobDef FindAnimalsToUnloadTogetherWith => FullDefsOf.ChapaGriff_UH_FindAnimalsToUnloadTogetherWith;
        private static List<JobDef> unloadingJobDefs = new List<JobDef>() { UnloadSelfAtSpot, UnloadRopeesAtSpot, RopeToPen_Extended, RopeRoamerToHitchingPost_Extended };
		public static List<JobDef> UnloadingJobDefs => unloadingJobDefs;

    }
}
