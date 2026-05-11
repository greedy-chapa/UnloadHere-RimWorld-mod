using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace ChapaGriff_UnloadHere
{
	[StaticConstructorOnStartup]
	public class Building_CaravanUnloadingSpot: Building, IStoreSettingsParent, ILoadReferenceable, IStorageGroupMember
	{
		public bool StorageTabVisible => true;
		public StorageGroup storageGroup;
		StorageGroup IStorageGroupMember.Group { get => storageGroup; set => storageGroup = value; }
		bool IStorageGroupMember.DrawConnectionOverlay => base.Spawned;
		Map IStorageGroupMember.Map => base.MapHeld;
		StorageSettings IStorageGroupMember.StoreSettings => GetStoreSettings();
		StorageSettings IStorageGroupMember.ParentStoreSettings => GetParentStoreSettings();
		StorageSettings IStorageGroupMember.ThingStoreSettings => settings;
		string IStorageGroupMember.StorageGroupTag => def.building.storageGroupTag;
		bool IStorageGroupMember.DrawStorageTab => true;
		bool IStorageGroupMember.ShowRenameButton => base.Faction == Faction.OfPlayer;
		public StorageSettings settings;
		private UnloadingMapComponent unloadingMC;
		public UnloadingMapComponent UnloadingMC
		{
			get
			{
				if (unloadingMC == null) unloadingMC = Map.GetComponent<UnloadingMapComponent>();
				return unloadingMC;
			}
		}
		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}
			foreach (Gizmo item in StorageSettingsClipboard.CopyPasteGizmosFor(GetStoreSettings()))
			{
				yield return item;
			}
			if (!StorageTabVisible || base.MapHeld == null)
			{
				yield break;
			}
			foreach (Gizmo item2 in StorageGroupUtility.StorageGroupMemberGizmos(this))
			{
				yield return item2;
			}
		}
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
			if (selPawn.CanReach(this, PathEndMode.Touch, Danger.Deadly))
			{
				if (selPawn.inventory.innerContainer.Any((Thing thing) => Accepts(thing)))
				{
					yield return new FloatMenuOption("ChapaGriff_UH.UnloadSelf".Translate(), delegate
					{
						var task = new Task_UnloadSelfAtSpot(this, selPawn);
						if (KeyBindingDefOf.QueueOrder.IsDown) //shift key is pressed
						{
							if (!UnloadingMC.taskLists.ContainsKey(selPawn)) UnloadingMC.taskLists[selPawn] = new TaskList();
                            UnloadingMC.taskLists[selPawn].Add(task);
							selPawn.jobs.jobQueue.EnqueueLast(task.MakeJob());
						}
						else
						{
							selPawn.jobs.StopAll();
							UnloadingMC.taskLists[selPawn] = new TaskList();
							UnloadingMC.taskLists[selPawn].Add(task);
							selPawn.jobs.TryTakeOrderedJob(task.MakeJob());
						}
					});
				}
				else yield return new FloatMenuOption("ChapaGriff_UH.UnloadSelf_Cannot".Translate("ChapaGriff_UH.NoAccepedThings".Translate()), null);
			}
			else yield return new FloatMenuOption("ChapaGriff_UH.UnloadSelf_Cannot".Translate("NoPath".Translate()), null);
        }
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref settings, "settings", this);
			Scribe_References.Look(ref storageGroup, "storageGroup");
		}
		public StorageSettings GetStoreSettings()
		{
			if (storageGroup != null)
			{
				return storageGroup.GetStoreSettings();
			}
			return settings;
		}
		public StorageSettings GetParentStoreSettings()
		{
			StorageSettings fixedStorageSettings = def.building.fixedStorageSettings;
			if (fixedStorageSettings != null)
			{
				return fixedStorageSettings;
			}
			return StorageSettings.EverStorableFixedSettings();
		}
		public void Notify_SettingsChanged() { }
		public bool Accepts(Thing t) => GetStoreSettings().AllowedToAccept(t);
		public override void PostMake()
		{
			base.PostMake();
			settings = new StorageSettings(this);
			if (def.building.defaultStorageSettings != null)
			{
				settings.CopyFrom(def.building.defaultStorageSettings);
			}
		}
		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (storageGroup != null && map != storageGroup.Map)
			{
				StorageSettings storeSettings = storageGroup.GetStoreSettings();
				storageGroup.RemoveMember(this);
				storageGroup = null;
				settings.CopyFrom(storeSettings);
			}
			UnloadingMC.spots.Add(this);
		}
		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			base.Destroy(mode);
			if (storageGroup != null)
			{
				storageGroup?.RemoveMember(this);
				storageGroup = null;
			}
			UnloadingMC.spots.Remove(this);
		}
		public override void DrawExtraSelectionOverlays()
		{
			base.DrawExtraSelectionOverlays();
			StorageGroupUtility.DrawSelectionOverlaysFor(this);
		}
		public override string GetInspectString()
		{
			var sb = new StringBuilder();
			sb.Append(base.GetInspectString());
			if (base.Spawned && storageGroup != null)
			{
				sb.AppendLineIfNotEmpty();
				sb.Append(storageGroup.RenamableLabel);
			}
			return sb.ToString();
		}
	}
}
