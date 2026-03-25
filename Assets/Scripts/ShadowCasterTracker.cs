using System.Collections.Generic;
using UnityEngine;

// Gắn script này vào bất kỳ vật nào muốn đổ bóng có fade theo khoảng cách.
// Không cần manager riêng - tất cả instance tự đồng bộ với nhau.
public class ShadowCasterTracker : MonoBehaviour
{
    const int MAX_CASTERS = 8;

    static readonly List<ShadowCasterTracker> s_All = new List<ShadowCasterTracker>();
    static readonly Vector4[] s_Positions = new Vector4[MAX_CASTERS];

    void OnEnable()
    {
        s_All.Add(this);
    }

    void OnDisable()
    {
        s_All.Remove(this);
    }

    void LateUpdate()
    {
        // chỉ instance đầu tiên trong list chịu trách nhiệm update shader
        if (s_All.Count == 0 || s_All[0] != this) return;

        int count = Mathf.Min(s_All.Count, MAX_CASTERS);
        for (int i = 0; i < count; i++)
            s_Positions[i] = s_All[i].transform.position;

        Shader.SetGlobalVectorArray("_ShadowCasterPositions", s_Positions);
        Shader.SetGlobalInt("_ShadowCasterCount", count);
    }
}
