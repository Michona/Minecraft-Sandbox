using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// This system updates all entities in the scene with both a RotationSpeed and Rotation component.
public class BoxCollidersSystem : JobComponentSystem
{
    NativeQueue<float3> addBoxQueue;

    GameObject colliderBox;

    protected override void OnCreate()
    {
        addBoxQueue = new NativeQueue<float3>(Allocator.TempJob);

        colliderBox = (GameObject)Resources.Load("ColliderBox", typeof(GameObject));
    }

    protected override void OnDestroy()
    {
        addBoxQueue.Dispose();
        base.OnDestroy();
    }

    [BurstCompile]
    [RequireComponentTag(typeof(BlockTag))]
    struct AddCollidersJob : IJobForEachWithEntity<Translation, ColliderComponent, PlayerDistance>
    {

        [WriteOnly]
        public NativeQueue<float3>.Concurrent AddBoxQueue;


        public void Execute(Entity entity, int jobIndex,
            [ReadOnly] ref Translation translation,
            ref ColliderComponent colliderData,
            [ReadOnly] ref PlayerDistance distance)
        {
            if (distance.Value < 2) {

                if (!colliderData.HasColliderBox) {
                    AddBoxQueue.Enqueue(translation.Value);
                    colliderData.HasColliderBox = true;
                }
            }
        }
    }


    // OnUpdate runs on the main thread.
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var colliderJob = new AddCollidersJob() {
            AddBoxQueue = addBoxQueue.ToConcurrent(),
        }.Schedule(this, inputDependencies);

        colliderJob.Complete();

        float3 translation;
        if (addBoxQueue.TryDequeue(out translation)) {

            if (colliderBox) {
                Object.Instantiate(colliderBox, translation, Quaternion.identity);
            }
        }
        return colliderJob;
    }
}
