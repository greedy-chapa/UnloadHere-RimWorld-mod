using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ChapaGriff_UnloadHere
{
	[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch(nameof(Pawn.GetFloatMenuOptions))]
	public class Pawn_GetFloatMenuOptions_Patch
	{
		public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> options, Pawn __instance, Pawn selPawn)
		{
			var pawn = __instance;
            foreach (var option in options) yield return option;
			if (pawn.Spawned && !selPawn.Drafted && UnloadingMapComponent.IsColonyAnimal(pawn) && pawn.RaceProps.packAnimal && AnimalPenUtility.IsRopeManagedAnimalDef(pawn.def))
			{
				if (selPawn.CanReach(pawn, PathEndMode.OnCell, Danger.Deadly))
				{
					if (!pawn.Downed)
					{
						if (pawn.MentalState == null)
						{
							var unloadingMC = pawn.Map.GetComponent<UnloadingMapComponent>();
							if (unloadingMC.spots.Any((Building spot) => selPawn.CanReach(spot, PathEndMode.Touch, Danger.Deadly)))
							{
								if (pawn.inventory.innerContainer.Any((Thing thing) => unloadingMC.AnyReachableSpotAccepts(thing, selPawn)))
								{
									var option = new FloatMenuOption("ChapaGriff_UH.UnloadAnimal".Translate(pawn.LabelShort), delegate
									{
										selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(UH_DefOf.ChapaGriff_UH_FindAnimalsToUnloadTogetherWith, pawn, pawn));
									});
									var reserved = pawn.Map.reservationManager.TryGetReserver(pawn, pawn.Faction, out Pawn reserver);
									if (reserved && reserver != selPawn) option.Label += ": " + "ReservedBy".Translate("", reserver);
									yield return option;
								}
								else yield return new FloatMenuOption("ChapaGriff_UH.UnloadAnimal_Cannot".Translate(pawn.LabelShort, "ChapaGriff_UH.NoAccepedThings".Translate()), null);
							}
							else yield return new FloatMenuOption("ChapaGriff_UH.UnloadAnimal_Cannot".Translate(pawn.LabelShort, "ChapaGriff_UH.NoReachableSpots".Translate()), null);
						}
						else yield return new FloatMenuOption("ChapaGriff_UH.UnloadAnimal_Cannot".Translate(pawn.LabelShort, "CantRopeAnimalMentalState".Translate(pawn, pawn.MentalStateDef)), null);
					}
					else yield return new FloatMenuOption("ChapaGriff_UH.UnloadAnimal_Cannot".Translate(pawn.LabelShort, "CantInteractAnimalDowned".Translate()), null);
                }
				else yield return new FloatMenuOption("ChapaGriff_UH.UnloadAnimal_Cannot".Translate(pawn.LabelShort, "NoPath".Translate()), null);
			}
		}
	}
}
