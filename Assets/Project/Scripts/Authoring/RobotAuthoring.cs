using UnityEngine;

public class RobotAuthoring : MonoBehaviour
{
    public float MetersPerSecond = 4f;
    public float RepathDelay = 1f;
    public GameObject Package;

    [Header("State Colors")]
    public Color ToShelfColor = new Color(0.216f, 0.541f, 0.867f);
    public Color PickingUpColor = new Color(0.29f, 0.565f, 0.886f);
    public Color TransportingColor = new Color(0.937f, 0.624f, 0.153f);
    public Color UnloadingColor = new Color(0.498f, 0.467f, 0.867f);
    public Color LoadingColor = new Color(0.851f, 0.467f, 0.341f);
    public Color ToChargerColor = new Color(0.886f, 0.294f, 0.29f);
    public Color ChargingColor = new Color(0.388f, 0.6f, 0.133f);
}
