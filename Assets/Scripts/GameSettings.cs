using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public GameObject PlayerPrefab;

    public GameObject WireframeBoxPrefab;

    public GameObject BlockDirtType;
    public GameObject BlockWaterType;
    public GameObject BlockFireType;
    public GameObject BlockSnowType;
    public GameObject BlockRockType;
    public GameObject BlockGrassType;

    public readonly Vector3 PlayerSpawnPosition = new Vector3(0, 10, 0);
}
