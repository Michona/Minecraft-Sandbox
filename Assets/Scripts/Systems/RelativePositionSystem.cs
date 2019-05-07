using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

/**
 * This system decides on which block entities are collider boxes going to be spawned.
 * It keeps the player position in sync on every entitiy.
 * And if the entity is close to the player it adds the nessesary components that in turn
 * initiate systems like CollidersSpawnerSystem.
 * 
 * The idea behind this is to limit the spawning of box collider game objects to only around the player.
 * This is a big optimization and its what allows to "spawn" huge number of block entities and only worry
 * about colliders around the player.
 * */
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BlocksSpawnerSystem))]
public class RelativePositionSystem : JobComponentSystem
{

    EntityCommandBufferSystem m_EntityCommandBufferSystem;

    private EntityQuery m_PlayerGroup;
    private NativeArray<float3> playerPositions;

    protected override void OnCreate()
    {
        m_PlayerGroup = GetEntityQuery(new EntityQueryDesc {
            All = new[] { ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<Translation>() }
        });

        playerPositions = new NativeArray<float3>(1, Allocator.Persistent);
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
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
    struct UpdateDistanceOnBlocksJob : IJobForEachWithEntity<Translation, PlayerPosition, ColliderData>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;

        [ReadOnly]
        public float3 PlayerPosition;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [WriteOnly] ref PlayerPosition p_Position, ref ColliderData colliderData)
        {
            p_Position.Value = PlayerPosition;

            if (math.distance(translation.Value.y, p_Position.Value.y) < 2 &&
                math.distance(translation.Value.x, p_Position.Value.x) < 3 &&
                math.distance(translation.Value.z, p_Position.Value.z) < 3) {
                if (!colliderData.HasColliderBox) {
                    CommandBuffer.AddComponent(index, entity, new SpawnColliderTag());
                    CommandBuffer.AddComponent(index, entity, new HasColliderTag());
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
        m_EntityCommandBufferSystem.AddJobHandleForProducer(positionJob);

        var distanceJob = new UpdateDistanceOnBlocksJob {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            PlayerPosition = playerPositions[0]
        }.Schedule(this, positionJob);

        return distanceJob;
    }
}