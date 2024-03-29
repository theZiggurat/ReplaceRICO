using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Prefabs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.InputSystem;

namespace ReplaceRICO
{
    internal partial class UpdateRicoSystem : GameSystemBase
    {
        private InputAction action;

        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_PrefabQuery;
        private EntityQuery m_BuildingQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            this.action = new InputAction("runReplacer", InputActionType.Button);
            this.action.AddCompositeBinding("OneModifier")
                .With("Binding", "<keyboard>/b")
                .With("Modifier", "<keyboard>/leftCtrl");

            m_PrefabSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<PrefabSystem>();
            m_PrefabQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<BuildingData>());
            m_BuildingQuery = GetEntityQuery(ComponentType.ReadWrite<Building>());
            RequireForUpdate(m_PrefabQuery);
            RequireForUpdate(m_BuildingQuery);
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            this.action.Enable();
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            this.action.Disable();
        }

        protected override void OnUpdate()
        {
            if (this.action.WasPressedThisFrame())
            {
                Mod.log.Info("Starting replacer");

                Dictionary<string, Entity> prefabs = new Dictionary<string, Entity>();

                NativeArray<Entity> prefabEntities = m_PrefabQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity prefabEntity in prefabEntities)
                {
                    if (!EntityManager.TryGetComponent(prefabEntity, out PrefabData prefabData))
                    {
                        Mod.log.Warn("Failed to query prefabData for entity: " + prefabEntity);
                        continue;
                    }
                    if (!m_PrefabSystem.TryGetPrefab(prefabData, out PrefabBase prefabBase))
                    {
                        Mod.log.Warn("Failed to query prefabBase for entity: " + prefabEntity + "\nPrefab data: " + prefabData);
                        continue;
                    }

                    if (prefabBase != null)
                    {
                        prefabs.Add(prefabBase.GetPrefabID().GetName() + "_ploppable", prefabEntity);
                    }
                }

                int valid = 0;
                int replaced = 0;
                int skipped = 0;
                NativeArray<Entity> buildingEntities = m_BuildingQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity buildingEntity in buildingEntities)
                {
                    if (!EntityManager.TryGetComponent(buildingEntity, out PrefabRef prefabRef))
                    {
                        Mod.log.Warn("Failed to query prefabRef for entity: " + buildingEntity);
                        skipped++;
                        continue;
                    }

                    if (!EntityManager.TryGetComponent(prefabRef.m_Prefab, out PrefabData prefabData))
                    {
                        Mod.log.Warn("Failed to query prefabData for entity: " + buildingEntity);
                        skipped++;
                        continue;
                    }

                    if (prefabData.m_Index > 0)
                    {
                        // valid building
                        valid++;
                        continue;
                    }

                    string prefabName = m_PrefabSystem.GetObsoleteID(prefabRef.m_Prefab).GetName();
                    if (!prefabs.ContainsKey(prefabName)) {
                        Mod.log.Warn($"Could not replace {buildingEntity}: {prefabName} not found");
                        skipped++;
                        continue;
                    }
                    Mod.log.Info($"Replacing {buildingEntity} with {prefabName}");
                    EntityManager.SetComponentData(buildingEntity, new PrefabRef(prefabs[prefabName]));
                    replaced++;
                }
                prefabEntities.Dispose();
                buildingEntities.Dispose();
                Enabled = false;

                Mod.log.Info($"Replacer finished. {replaced} replaced, {skipped} skipped, {valid} already valid");
            }
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 1;
        }
    }
}