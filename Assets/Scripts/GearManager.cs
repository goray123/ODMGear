using UnityEngine;

public class GearManager : MonoBehaviour
{
    [Header("Gear")]
    [SerializeField] private GameObject gearPrefab;
    [SerializeField] private float gearSpeed = 40f;
    [SerializeField] private LayerMask attachMask = ~0;

    private GearBehaviour activeGear;

    public void FireGear(Vector3 origin, Vector3 direction, PlayerController owner)
    {
        if (gearPrefab == null)
        {
            Debug.LogWarning("GearManager: gearPrefab is not assigned.");
            return;
        }

        if (activeGear != null)
        {
            Destroy(activeGear.gameObject);
            activeGear = null;
        }
        
        GameObject gearInstance = Instantiate(gearPrefab, origin, Quaternion.LookRotation(direction));
        activeGear = gearInstance.GetComponent<GearBehaviour>();
        Debug.Log("Instantiated gear prefab.");

        if (activeGear == null)
        {
            Debug.LogError("GearManager: gearPrefab does not contain a GearBehaviour component.");
            return;
        }

        activeGear.Initialize(owner, direction, gearSpeed, attachMask);
    }
}
