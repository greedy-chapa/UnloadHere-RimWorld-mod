using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace ChapaGriff_UnloadHere
{
	[HarmonyPatch(typeof(CaravanEnterMapUtility))]
	[HarmonyPatch(nameof(CaravanEnterMapUtility.Enter))]
	[HarmonyPatch(new Type[] {typeof(Caravan), typeof(Map), typeof(Func<Pawn, IntVec3>), typeof(CaravanDropInventoryMode), typeof(bool)})]
	public class CaravanEnterMapUtility_Enter_Patch
	{
		public static bool Prefix(Caravan caravan, Map map, Func<Pawn, IntVec3> spawnCellGetter, ref List<Pawn> ___tmpPawns, CaravanDropInventoryMode dropInventoryMode = CaravanDropInventoryMode.DoNotDrop, bool draftColonists = false)
		{
			var traverse = Traverse.Create(typeof(CaravanEnterMapUtility));
            ___tmpPawns.Clear();
            ___tmpPawns.AddRange(caravan.PawnsListForReading);

			Building_PassengerShuttle shuttle = caravan.Shuttle;
			if (shuttle != null)
			{
				CaravanInventoryUtility.GetOwnerOf(caravan, shuttle).inventory.innerContainer.Remove(shuttle);
			}
			for (int i = 0; i < ___tmpPawns.Count; i++)
			{
				IntVec3 loc = spawnCellGetter(___tmpPawns[i]);
				GenSpawn.Spawn(___tmpPawns[i], loc, map, Rot4.Random);
			}
			if (shuttle != null)
			{
				if (!CellFinder.TryFindRandomCellNear(___tmpPawns.RandomElement().Position, map, 10, (IntVec3 c) => RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(c, map), out var result))
				{
					result = ___tmpPawns.RandomElement().Position;
				}
				GenPlace.TryPlaceThing(shuttle, result, map, ThingPlaceMode.Near);
				CaravanShuttleUtility.LoadCaravanItemsIntoContainer(___tmpPawns, shuttle.TransporterComp.innerContainer);
			}

			switch (dropInventoryMode)
			{
				case CaravanDropInventoryMode.DropInstantly:
					CaravanEnterMapUtility.DropAllInventory(___tmpPawns);
					break;
				case CaravanDropInventoryMode.UnloadIndividually:
					map.GetComponent<UnloadingMapComponent>().Unload(___tmpPawns, draftColonists); //my line of code
					break;
			}

			if (draftColonists)
			{
				traverse.Method("DraftColonists", ___tmpPawns).GetValue();
			}
			if (!draftColonists && map.IsPlayerHome && dropInventoryMode != CaravanDropInventoryMode.UnloadIndividually)
			{
				CaravanFormingUtility.LeadAnimalsToPen(___tmpPawns);
			}
			if (map.IsPlayerHome)
			{
				for (int num2 = 0; num2 < ___tmpPawns.Count; num2++)
				{
					if (___tmpPawns[num2].IsPrisoner)
					{
						___tmpPawns[num2].guest.WaitInsteadOfEscapingForDefaultTicks();
					}
				}
			}

			caravan.RemoveAllPawns();
			if (!caravan.Destroyed)
			{
				caravan.Destroy();
			}

			___tmpPawns.Clear();

			return false;
		}
	}
}
