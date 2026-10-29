using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ChapaGriff_UnloadHere
{
	[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch(nameof(Pawn.GetGizmos))]
	public class Pawn_GetGizmos_Patch
	{
		public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Pawn __instance)
		{
			var pawn = __instance;
            foreach (Gizmo gizmo in gizmos) yield return gizmo;
			if (pawn.Spawned && (pawn.IsColonistPlayerControlled || pawn.IsColonyMechPlayerControlled || (UnloadingMapComponent.IsColonyAnimal(pawn) && pawn.RaceProps.packAnimal && !AnimalPenUtility.IsRopeManagedAnimalDef(pawn.def))))
			{
				var unloadingMC = pawn.Map.GetComponent<UnloadingMapComponent>();
				var gizmo = new Command_Action()
				{
					defaultLabel = "ChapaGriff_UH.UnloadSelf".Translate(),
					defaultDesc = "ChapaGriff_UH.UnloadSelf_Description".Translate(),
					icon = ContentFinder<Texture2D>.Get("ChapaGriff.UnloadHere/UnloadGizmo"),
					action = delegate
					{
						pawn.jobs.StopAll();
						unloadingMC.GetTaskListForSelfUnloading(pawn).Execute(pawn);
					}
				};
				if (pawn.Downed) gizmo.Disable("DownedLower".Translate());
				else if (pawn.MentalStateDef != null) gizmo.Disable("CantRopeAnimalMentalState".Translate(pawn, pawn.MentalStateDef));
				else if (!unloadingMC.spots.Any((Building spot) => pawn.CanReach(spot, PathEndMode.Touch, Danger.Deadly)))
					gizmo.Disable("ChapaGriff_UH.NoReachableSpots".Translate());
				else if (!pawn.inventory.innerContainer.Any((Thing thing) => unloadingMC.AnyReachableSpotAccepts(thing, pawn)))
					gizmo.Disable("ChapaGriff_UH.NoAccepedThings".Translate());
				yield return gizmo;
            }
        }

	}
}
