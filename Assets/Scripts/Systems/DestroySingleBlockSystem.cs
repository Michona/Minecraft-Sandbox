using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BlockSpawnerSystem))]
[UpdateAfter(typeof(DistanceToPlayerSystem))]
public class DestroySingleBlockSystem : JobComponentSystem
{

    EntityCommandBufferSystem m_EntityCommandBufferSystem;
    private EntityQuery m_DestroyBlockGroup;
    private NativeArray<float3> destroyedBlockPositions;

    protected override void OnCreate()
    {
        m_DestroyBlockGroup = GetEntityQuery(new EntityQueryDesc {
            All = new [] { ComponentType.ReadOnly<DestroyBlockTag>(), ComponentType.ReadOnly<Translation>() }
        });
        destroyedBlockPositions = new NativeArray<float3>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnStopRunning()
    {
        destroyedBlockPositions.Dispose();
        base.OnStopRunning();
    }

    struct GetDestroyBlockPositionJob : IJobForEachWithEntity<Translation>
    {
        [WriteOnly]
        public NativeArray<float3> TargetPosition;
        public EntityCommandBuffer.Concurrent CommandBuffer;


        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation)
        {
            TargetPosition[index] = translation.Value;
            CommandBuffer.DestroyEntity(index, entity);
        }
    }

    [RequireComponentTag(typeof(BlockTag))]
    struct DestroySingleBlockSystemJob : IJobForEachWithEntity<Translation>
    {
        [ReadOnly]
        public float3 TargetPosition;
        public EntityCommandBuffer.Concurrent CommandBuffer;


        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation)
        {
            if (math.distance(translation.Value, TargetPosition) < 1) {
                CommandBuffer.DestroyEntity(index, entity);
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var getDestroyBlockPositionJob = new GetDestroyBlockPositionJob {
            TargetPosition = destroyedBlockPositions,
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(m_DestroyBlockGroup, inputDependencies);

        getDestroyBlockPositionJob.Complete();
        if (m_DestroyBlockGroup.CalculateLength() > 0) {

            var job = new DestroySingleBlockSystemJob {
                TargetPosition = destroyedBlockPositions[0],
                CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
            }.Schedule(this, getDestroyBlockPositionJob);

            return job;
        }

        // Now that the job is set up, schedule it to be run. 
        return getDestroyBlockPositionJob;
    }
}