using UnityEngine;

public class RotateY : MonoBehaviour
{
    [Tooltip("Độ xoay mỗi giây (dương = thuận chiều kim đồng hồ nhìn từ trên xuống)")]
    public float speed = 90f;

    void Update()
    {
        transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);
    }
}
