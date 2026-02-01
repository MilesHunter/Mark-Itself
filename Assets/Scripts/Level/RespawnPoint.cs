using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    // 是否为默认重生点，玩家初始会定位到此
    public bool isDefaultSpawn = false;

    // 可以选择在 Inspector 中显示重生点的图标
    void OnDrawGizmos()
    {
    #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, gameObject.name + (isDefaultSpawn ? " (Default)" : ""));
    #endif
    }
}