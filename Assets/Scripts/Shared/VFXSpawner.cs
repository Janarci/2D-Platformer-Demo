using UnityEngine;

public sealed class VFXSpawner : MonoBehaviour
{
    [SerializeField] private Transform defaultParent;

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
}
