using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

public sealed class GameInstance
{
    public static GameSettings Settings;

    public static void StartGame()
    {
        var player = Object.Instantiate(Settings.PlayerPrefab, new Vector3(0, 20, 0), Quaternion.identity);
        var entity = player.GetComponent<GameObjectEntity>().Entity;
        var entityManager = World.Active.EntityManager;
        entityManager.AddComponentData(entity, new PlayerTag());
        entityManager.AddComponentData(entity, new Translation { Value = player.transform.position }); 
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitializeWithScene()
    {
        var settingsGo = GameObject.Find("GameSettings");
        Settings = settingsGo?.GetComponent<GameSettings>();
    }
}
