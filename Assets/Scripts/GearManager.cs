using UnityEngine;

public enum GearSlot
{
    Left,
    Right
}

public class GearManager : MonoBehaviour
{
    [Header("Gear")]
    [SerializeField] private GameObject gearPrefab;
    [SerializeField] private float gearSpeed = 40f;
    [SerializeField] private LayerMask attachMask = ~0;

    private GearBehaviour leftGear;
    private GearBehaviour rightGear;

    public bool IsAnchorAttached => IsGearAnchored(leftGear) || IsGearAnchored(rightGear);
    public bool IsLeftAnchorAttached => IsGearAnchored(leftGear);
    public bool IsRightAnchorAttached => IsGearAnchored(rightGear);
    public int AnchoredGearCount => GetAnchoredGearCount();

    public void FireGear(GearSlot slot, Vector3 origin, Vector3 direction, PlayerController owner)
    {
        if (gearPrefab == null)
        {
            Debug.LogWarning("GearManager: gearPrefab is not assigned.");
            return;
        }

        ReleaseGear(slot);
        
        GameObject gearInstance = Instantiate(gearPrefab, origin, Quaternion.LookRotation(direction));
        GearBehaviour gear = gearInstance.GetComponent<GearBehaviour>();
        Debug.Log("Instantiated gear prefab.");

        if (gear == null)
        {
            Debug.LogError("GearManager: gearPrefab does not contain a GearBehaviour component.");
            Destroy(gearInstance);
            return;
        }

        SetGear(slot, gear);
        gear.Initialize(owner, direction, gearSpeed, attachMask);
    }

    public void ReleaseGear(GearSlot slot)
    {
        GearBehaviour gear = GetGear(slot);

        if (gear == null)
            return;

        Destroy(gear.gameObject);
        SetGear(slot, null);
    }

    public void ReleaseGear()
    {
        ReleaseGear(GearSlot.Left);
        ReleaseGear(GearSlot.Right);
    }

    private GearBehaviour GetGear(GearSlot slot)
    {
        return slot == GearSlot.Left ? leftGear : rightGear;
    }

    private void SetGear(GearSlot slot, GearBehaviour gear)
    {
        if (slot == GearSlot.Left)
            leftGear = gear;
        else
            rightGear = gear;
    }

    private int GetAnchoredGearCount()
    {
        int count = 0;

        if (IsGearAnchored(leftGear))
            count++;

        if (IsGearAnchored(rightGear))
            count++;

        return count;
    }

    private bool IsGearAnchored(GearBehaviour gear)
    {
        return gear != null && gear.IsAnchored;
    }
}
