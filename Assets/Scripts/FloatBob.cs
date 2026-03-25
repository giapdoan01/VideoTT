using UnityEngine;

public class FloatBob : MonoBehaviour
{
    [Tooltip("Biên độ bay lên/xuống (đơn vị Unity)")]
    public float amplitude = 0.3f;

    [Tooltip("Tốc độ dao động")]
    public float frequency = 1.5f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) * amplitude;
        transform.position = startPos + new Vector3(0f, offset, 0f);
    }
}
