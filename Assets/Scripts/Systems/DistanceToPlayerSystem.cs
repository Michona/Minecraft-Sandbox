using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BlockSpawnerSystem))]
[UpdateBefore(typeof(DestroySingleBlockSystem))]
public class DistanceToPlayerSystem : JobComponentSystem
{

    EntityCommandBufferSystem m_EntityCommandBufferSystem;

    private EntityQuery m_PlayerGroup;
    private NativeArray<float3> playerPositions;

    protected override void OnCreate()
    {
        m_PlayerGroup = GetEntityQuery(new EntityQueryDesc {
            All = new[] { ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<Translation>() }
        });

        playerPositions = new NativeArray<float3>(1 ,Allocator.TempJob);
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }


    protected override void OnStopRunning()
    {
        playerPositions.Dispose();
        base.OnStopRunning();
    }

    [BurstCompile]
    [RequireComponentTag(typeof(PlayerTag))]
    struct GetPlayerPositionJob : IJobForEachWithEntity<Translation>
    {
        [WriteOnly]
        public NativeArray<float3> PlayerPositions;

        public void Execute(Entity entity, int index, ref Translation traslation)
        {
            PlayerPositions[index] = traslation.Value;
        }
    }

    [RequireComponentTag(typeof(BlockTag))]
    struct UpdateDistanceOnBlocksJob : IJobForEachWithEntity<Translation, PlayerDistance, ColliderData>
    {

        public EntityCommandBuffer.Concurrent CommandBuffer;

        [ReadOnly]
        public float3 PlayerPosition;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [WriteOnly] ref PlayerDistance distance, ref ColliderData colliderData)
        {
            distance.Value = math.distance(translation.Value, PlayerPosition);

            if (distance.Value < 3) {
                if (!colliderData.HasColliderBox) {
                    CommandBuffer.AddComponent(index, entity, new SpawnColliderBoxTag());
                    colliderData.HasColliderBox = true;
                }
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var positionJob = new GetPlayerPositionJob {
            PlayerPositions = playerPositions
        }.Schedule(m_PlayerGroup, inputDependencies);

        positionJob.Complete();


        var distanceJob = new UpdateDistanceOnBlocksJob {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            PlayerPosition = playerPositions[0]
        }.Schedule(this, positionJob);

        return distanceJob;
    }
}