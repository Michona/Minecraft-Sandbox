using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/**
 * Operates on entities with HasColliderTag added.
 * Adds their position in native queue container 
 * if they are far away from the player, and it 
 * removes the ColliderBox (GameObject with only collider box attached) in that position.
 * 
 * The number of entities that get the HasColliderTag added is relatevly small,
 * @see DistanceToPlayerSystem.
 * 
 * The job runs really fast (we can use BurstCompile since there is no adding/removing components) and we wait for completion on the main thread where 
 * the collider boxes are destroyed.
 * 
 */
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BlocksSpawnerSystem))]
[UpdateAfter(typeof(RelativePositionSystem))]
public class DestroyCollidersTag : JobComponentSystem
{
    private NativeQueue<float3> colliderPositions;
    private EntityQuery m_CollidersGroup;

    protected override void OnCreate()
    {
        m_CollidersGroup = GetEntityQuery(new EntityQueryDesc {
            All = new[] { ComponentType.ReadOnly<HasColliderTag>(), 
            ComponentType.ReadOnly<Translation>(), 
            ComponentType.ReadOnly<PlayerPosition>(), 
            ComponentType.ReadWrite<ColliderData>() }
        });

        colliderPositions = new NativeQueue<float3>(Allocator.Persistent);
    }

    [BurstCompile]
    struct DestroyCollidersTagJob : IJobForEach<Translation, PlayerPosition, ColliderData>
    {
        [WriteOnly]
        public NativeQueue<float3>.Concurrent Positions;

        [ReadOnly]
        public int FieldSize;

        public void Execute([ReadOnly] ref Translation translation, [ReadOnly] ref PlayerPosition playerPosition, [WriteOnly] ref ColliderData colliderData)
        {
            if (math.distance(translation.Value.z, playerPosition.Value.z) > 5 || math.distance(translation.Value.x, playerPosition.Value.x) > 5) {
                colliderData.HasColliderBox = false;
                Positions.Enqueue(translation.Value);
            }
        }
    }

    protected override void OnDestroy()
    {
        colliderPositions.Dispose();
        base.OnDestroy();
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new DestroyCollidersTagJob{ 
            Positions = colliderPositions.ToConcurrent(),    
            FieldSize = GameInstance.Settings.FieldSize
        }.Schedule(m_CollidersGroup, inputDependencies);
   
        job.Complete();

        float3 removePosition;

        while (colliderPositions.Count > 0) {
            if (colliderPositions.TryDequeue(out removePosition)) {
                var raycastPos = new Vector3(removePosition.x, removePosition.y - 1f, removePosition.z);
                RaycastHit hit;
                if (Physics.Raycast(raycastPos, Vector3.up, out hit, 0.5f)) {
                    Object.Destroy(hit.transform.gameObject);
                }
            }
        }
        return job;
    }
}