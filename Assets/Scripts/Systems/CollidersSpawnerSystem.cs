using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/**
 * Operates on entities with SpawnColliderTag added.
 * Adds their position in native array container and 
 * instantiates a ColliderBox (GameObject with only collider box attached) in that position.
 * 
 * The number of entities that get the SpawnColliderTag added is relatevly small,
 * @see DistanceToPlayerSystem.
 * 
 * The job runs really fast and we wait for completion on the main thread where 
 * the collider boxes are spawned.
 * It removes the SpawmColliderTag once the entity position has been added to the array.
 */
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BlocksSpawnerSystem))]
public class CollidersSpawnerSystem : JobComponentSystem
{
    private NativeArray<float3> boxesToAdd;
    private GameObject colliderBox;

    EntityCommandBufferSystem m_EntityCommandBufferSystem;

    private EntityQuery m_CollidersGroup;

    protected override void OnCreate()
    {
        colliderBox = (GameObject)Resources.Load("ColliderBox", typeof(GameObject));

        m_CollidersGroup = GetEntityQuery(new EntityQueryDesc {
            All = new[] { ComponentType.ReadOnly<SpawnColliderTag>(), ComponentType.ReadOnly<Translation>() }
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
            CommandBuffer.RemoveComponent(index, entity, typeof(SpawnColliderTag));
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        if (!boxesToAdd.IsCreated) {
            boxesToAdd = new NativeArray<float3>(m_CollidersGroup.CalculateLength(), Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        }

        var colliderJob = new AddCollidersJob() {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            BoxesPositions = boxesToAdd,
        }.Schedule(m_CollidersGroup, inputDependencies);

        colliderJob.Complete();

        if (m_CollidersGroup.CalculateLength() > 0) {
            for (int i = 0; i < boxesToAdd.Length; i++) {
                if (colliderBox) {
                    Object.Instantiate(colliderBox, boxesToAdd[i], Quaternion.identity);
                }
            }
        }
        boxesToAdd.Dispose();

        return colliderJob;
    }
}
