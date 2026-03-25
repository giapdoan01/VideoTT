// Gắn script này vào bất kỳ GameObject nào muốn đổ bóng cloud lên cỏ.
// Yêu cầu: GameObject phải có MeshFilter.
// Script tự tạo một mesh con ẩn trên layer "CloudShadow" để camera bóng render.
// Mesh con tự đồng bộ vị trí/xoay/scale vì là child của cloud.
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
public class CloudShadowCaster : MonoBehaviour
{
    [Tooltip("Material dùng shader Hidden/CloudShadowCaster")]
    public Material shadowMaterial;

    GameObject _shadowObj;

    void OnEnable()  => CreateShadowMesh();
    void OnDisable() => DestroyShadowMesh();

    void CreateShadowMesh()
    {
        int layer = LayerMask.NameToLayer("CloudShadow");
        if (layer < 0)
        {
            Debug.LogError("[CloudShadowCaster] Chưa có layer 'CloudShadow'. " +
                           "Vào Edit > Project Settings > Tags and Layers để tạo.");
            return;
        }

        if (shadowMaterial == null)
        {
            Debug.LogError("[CloudShadowCaster] Chưa gán shadowMaterial " +
                           "(dùng shader Hidden/CloudShadowCaster).");
            return;
        }

        var mf = GetComponent<MeshFilter>();
        if (mf == null) return;

        // tạo child object ẩn, chỉ visible với camera bóng (layer CloudShadow)
        _shadowObj = new GameObject("_ShadowMesh")
        {
            layer     = layer,
            hideFlags = HideFlags.HideAndDontSave
        };
        _shadowObj.transform.SetParent(transform, false);

        // copy mesh từ cloud gốc
        var childMF = _shadowObj.AddComponent<MeshFilter>();
        childMF.sharedMesh = mf.sharedMesh;

        // render với CloudShadowCaster shader, không cast/receive shadow thông thường
        var mr = _shadowObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial    = shadowMaterial;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows    = false;
    }

    void DestroyShadowMesh()
    {
        if (_shadowObj == null) return;
        if (Application.isPlaying) Destroy(_shadowObj);
        else                       DestroyImmediate(_shadowObj);
        _shadowObj = null;
    }
}
