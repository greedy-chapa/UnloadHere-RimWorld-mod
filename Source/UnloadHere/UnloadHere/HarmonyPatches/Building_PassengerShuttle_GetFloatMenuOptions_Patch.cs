using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ChapaGriff_UnloadHere
{
	[HarmonyPatch(typeof(ThingWithComps))]
	[HarmonyPatch(nameof(ThingWithComps.GetFloatMenuOptions))]
	public class Building_PassengerShuttle_GetFloatMenuOptions_Patch
	{
		public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> options, ThingWithComps __instance, Pawn selPawn)
		{
			var thing = __instance;
			foreach (var option in options) yield return option;
			if (ModsConfig.IsActive("ludeon.rimworld.odyssey"))
			{
				foreach (var option in GetFloatMenuOptionsForShuttle(thing, selPawn)) yield return option;
			}
		}
		private static IEnumerable<FloatMenuOption> GetFloatMenuOptionsForShuttle(ThingWithComps thing, Pawn selPawn)
		{
			if (thing is Building_PassengerShuttle shuttle && shuttle.Spawned && shuttle.ShuttleComp.IsPlayerShuttle)
			{
				if (selPawn.CanReach(shuttle, PathEndMode.InteractionCell, Danger.Deadly))
				{
					var unloadingMC = shuttle.Map.GetComponent<UnloadingMapComponent>();
					if (unloadingMC.spots.Any((Building spot) => selPawn.CanReach(spot, PathEndMode.Touch, Danger.Deadly)))
					{
						if (shuttle.TransporterComp.innerContainer.Any((Thing item) => !selPawn.Map.reservationManager.IsReserved(item) && unloadingMC.AnyReachableSpotAccepts(item, selPawn)))
						{
							yield return new FloatMenuOption("ChapaGriff_UH.UnloadAnimal".Translate(shuttle.Label), delegate
							{
								selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(UH_DefOf.ChapaGriff_UH_UnloadShuttle, shuttle));
							});
						}
						else yield return new FloatMenuOption("ChapaGriff_UH.UnloadAnimal_Cannot".Translate(shuttle.Label, "ChapaGriff_UH.NoAccepedThings".Translate()), null);
					}
					else yield return new FloatMenuOption("ChapaGriff_UH.UnloadAnimal_Cannot".Translate(shuttle.Label, "ChapaGriff_UH.NoReachableSpots".Translate()), null);
				}
				else yield return new FloatMenuOption("ChapaGriff_UH.UnloadAnimal_Cannot".Translate(shuttle.Label, "NoPath".Translate()), null);
			}
		}
	}
}
