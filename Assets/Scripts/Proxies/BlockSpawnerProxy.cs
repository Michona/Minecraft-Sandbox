using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[RequiresEntityConversion]
public class BlockSpawnerProxy : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject DirtPrefab;
    public GameObject GrassPrefab;
    public GameObject SnowPrefab;
    public int CountX;
    public int CountY;

    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
    {
        gameObjects.Add(DirtPrefab);
        gameObjects.Add(GrassPrefab);
        gameObjects.Add(SnowPrefab);
    }

    // Lets you convert the editor data representation to the entity optimal runtime representation

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var spawnerData = new BlockSpawner {
            // The referenced prefab will be converted due to DeclareReferencedPrefabs.
            // So here we simply map the game object to an entity reference to that prefab.
            DirtPrefab = conversionSystem.GetPrimaryEntity(DirtPrefab),
            GrassPrefab = conversionSystem.GetPrimaryEntity(GrassPrefab),
            SnowPrefab = conversionSystem.GetPrimaryEntity(SnowPrefab),
            CountX = CountX,
            CountY = CountY
        };
        dstManager.AddComponentData(entity, spawnerData);
    }
}
