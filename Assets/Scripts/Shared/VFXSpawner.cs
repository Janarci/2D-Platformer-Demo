using UnityEngine;

public sealed class VFXSpawner : MonoBehaviour
{
    [SerializeField] private Transform defaultParent;
    [SerializeField] private GameObject damagedVfxPrefab;
    [SerializeField] private Transform damagedVfxSpawnPoint;
    [SerializeField] private Vector3 damagedVfxOffset;
    
    
    public GameObject Spawn(GameObject prefab)
    {
        return Spawn(prefab, transform.position, transform.rotation, defaultParent);
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null)
        {
            return null;
        }

        return Instantiate(prefab, position, rotation, parent != null ? parent : defaultParent);
    }
    
    public void SpawnDamagedVfx()
    {
        Debug.Log("Spawning damaged VFX");
        if (damagedVfxPrefab == null)
        {
            return;
        }

        var spawnTransform = damagedVfxSpawnPoint != null
            ? damagedVfxSpawnPoint
            : transform;

        Spawn(
            damagedVfxPrefab,
            spawnTransform.position + damagedVfxOffset,
            damagedVfxPrefab.transform.rotation,
            null);
    }
    
}
