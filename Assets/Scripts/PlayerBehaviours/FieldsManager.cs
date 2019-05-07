using Unity.Mathematics;
using UnityEngine;

/**
 * "Field" means a big block of small block entities. (20x20, 30x30 ..) (didn't have a better name :)) 
 * When the player moves and he leaves the current field, new one is generated on a position 
 * calculated from this class. 
 * It's attached to the player prefab.
 * */
public class FieldsManager : MonoBehaviour
{

    private int fieldSize;
    private float2 currentField;

    void Start()
    {
        fieldSize = GameInstance.Settings.FieldSize;
        currentField = new float2(GameInstance.Settings.FieldSize / 2, GameInstance.Settings.FieldSize / 2);
    }

    void FixedUpdate()
    {
        if (transform.position.x == 0 || transform.position.z == 0) {
            return;
        }

        float midX = GetMidLocationOfCurrent(transform.position.x);
        float midZ = GetMidLocationOfCurrent(transform.position.z);
        float2 nextField = new float2(midX, midZ);

        if (!(currentField.x == nextField.x && currentField.y == nextField.y)) {
            GenerateTerrainProxy.Instance.GenerateTerrain(nextField);
            currentField = nextField;
        }
    }

    private int GetAdditiveInverse(float position)
    {
        return position < 0 ? -1 : 1;
    }

    private int GetNextBorder(float position)
    {
        int nextBorder = (int)(position / fieldSize);
        nextBorder = (nextBorder + GetAdditiveInverse(position)) * fieldSize;

        return nextBorder;
    }

    private int GetMidLocationOfCurrent(float position)
    {
        return (GetNextBorder(position) + GetNextBorder(position) - GetAdditiveInverse(position) * fieldSize) / 2;
    }
}
