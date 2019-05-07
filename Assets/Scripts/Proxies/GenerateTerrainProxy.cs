using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/* Singleton */
public class GenerateTerrainProxy
{
    private static GenerateTerrainProxy _instance = null;
    public static GenerateTerrainProxy Instance {
        get { 
            if (_instance == null) {
                _instance = new GenerateTerrainProxy();
            } 
            return _instance;
        }
    }

    private Entity dirtBlock;
    private Entity grassBlock;
    private Entity snowBlock;

    private EntityManager manager;

    /* Initialize all the Entity used when the game starts. */
    public void StartGame()
    {
        dirtBlock = GameObjectConversionUtility.ConvertGameObjectHierarchy(GameInstance.Settings.BlockDirtType, World.Active);
        grassBlock = GameObjectConversionUtility.ConvertGameObjectHierarchy(GameInstance.Settings.BlockGrassType, World.Active);
        snowBlock = GameObjectConversionUtility.ConvertGameObjectHierarchy(GameInstance.Settings.BlockSnowType, World.Active);

        manager = World.Active.EntityManager;

        GenerateTerrain(new float2(GameInstance.Settings.FieldSize / 2, GameInstance.Settings.FieldSize / 2));
    }

    /* Creates entity with BlockSpawner component so that BlocksSpawnerSystem can process it. */
    public void GenerateTerrain(float2 position)
    {
        var entity = manager.CreateEntity();

        manager.AddComponentData(entity, new BlockSpawner {
            DirtPrefab = dirtBlock,
            GrassPrefab = grassBlock,
            SnowPrefab = snowBlock,
            FieldSize = GameInstance.Settings.FieldSize,
            MiddlePosition = position
        });
        manager.AddComponentData(entity, new LocalToWorld { Value = new float4x4(Quaternion.identity, float3.zero) });
    }
}
