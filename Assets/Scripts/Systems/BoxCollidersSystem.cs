using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(BlockSpawnerSystem))]
public class BoxCollidersSystem : JobComponentSystem
{
    private NativeArray<float3> boxesToAdd;
    private GameObject colliderBox;

    EntityCommandBufferSystem m_EntityCommandBufferSystem;

    private EntityQuery m_CollidersGroup;

    protected override void OnCreate()
    {
        colliderBox = (GameObject)Resources.Load("ColliderBox", typeof(GameObject));

        m_CollidersGroup = GetEntityQuery(new EntityQueryDesc {
            All = new[] { ComponentType.ReadOnly<SpawnColliderBoxTag>(), ComponentType.ReadOnly<Translation>() }
        });

        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnStopRunning()
    {
        if (boxesToAdd.IsCreated) {
            boxesToAdd.Dispose();
        }
        base.OnStopRunning();
    }

    [RequireComponentTag(typeof(BlockTag))]
    struct AddCollidersJob : IJobForEachWithEntity<Translation>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;

        [WriteOnly]
        public NativeArray<float3> BoxesPositions;

        public void Execute(Entity entity, int index, ref Translation translation)
        {
            BoxesPositions[index] = translation.Value;
            CommandBuffer.RemoveComponent(index, entity, typeof(SpawnColliderBoxTag));
        }
    }


    // OnUpdate runs on the main thread.
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {

        boxesToAdd = new NativeArray<float3>(m_CollidersGroup.CalculateLength(), Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        var colliderJob = new AddCollidersJob() {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            BoxesPositions = boxesToAdd,
        }.Schedule(m_CollidersGroup, inputDependencies);

        colliderJob.Complete();

        for (int i = 0; i < boxesToAdd.Length; i ++) {
            if (colliderBox) {
                Object.Instantiate(colliderBox, boxesToAdd[i], Quaternion.identity);
            }
        }
        boxesToAdd.Dispose();

        return colliderJob;
    }
}
