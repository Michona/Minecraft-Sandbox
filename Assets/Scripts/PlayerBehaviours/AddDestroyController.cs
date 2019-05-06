using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

public class AddDestroyController : MonoBehaviour
{
    private GameObject wireframeBox;

    private EntityManager manager;

    private Entity blockEntity;

    void OnEnable()
    {
        wireframeBox = Object.Instantiate(GameInstance.Settings.WireframeBoxPrefab, Vector3.zero, Quaternion.identity);
        Assert.IsNotNull(wireframeBox);

        blockEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(GameInstance.Settings.BlockDirtType, World.Active);
        manager = World.Active.EntityManager;
    }


    void Update()
    {
        WireframeUpdate();

        if (Input.GetMouseButtonDown(0)) {
            if (IsBlockAllowedToSpawn(wireframeBox.transform.position)) {
                SpawnBlockEntity(wireframeBox.transform.position);
            }
        }

        if (Input.GetMouseButtonDown(1)) {
            DestroyBlockEntity(wireframeBox.transform.position);
        }
    }

    private void WireframeUpdate()
    {
        var currentPos = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, Camera.main.nearClipPlane + 2));
        wireframeBox.transform.position = new Vector3(Mathf.Round(currentPos.x),
                                         Mathf.Round(currentPos.y),
                                         Mathf.Round(currentPos.z));

        wireframeBox.transform.eulerAngles = new Vector3(0, 0, 0);
    }

    private void SpawnBlockEntity(Vector3 position)
    {
        var instance = manager.Instantiate(blockEntity);
        manager.SetComponentData(instance, new Translation { Value = position });
        manager.AddComponentData(instance, new BlockTag());
        manager.AddComponentData(instance, new ColliderData{ HasColliderBox = false });
        manager.AddComponentData(instance, new PlayerDistance{ Value = 0 });
    }

    private bool IsBlockAllowedToSpawn(Vector3 position)
    {
        RaycastHit hit;
        return (Physics.Raycast(position, Vector3.down, out hit, 0.5f) ||
                Physics.Raycast(position, Vector3.up, out hit, 0.5f) ||
                Physics.Raycast(position, Vector3.left, out hit, 0.5f) ||
                Physics.Raycast(position, Vector3.right, out hit, 0.5f) ||
                Physics.Raycast(position, Vector3.forward, out hit, 0.5f) ||
                Physics.Raycast(position, Vector3.back, out hit, 0.5f)); 
    }

    private void DestroyBlockEntity(Vector3 position)
    {
        var raycastPos = new Vector3(position.x, position.y - 1f, position.z);
        RaycastHit hit;
        if (Physics.Raycast(raycastPos, Vector3.up, out hit, 0.5f)) {

            Destroy(hit.transform.gameObject);

            var instance = manager.Instantiate(blockEntity);
            manager.SetComponentData(instance, new Translation {Value = position} );
            manager.AddComponentData(instance, new DestroyBlockTag());
        }
    }
   
}
