// Đặt script này vào một GameObject rỗng trong scene (chỉ cần 1 instance).
// Script tự tạo camera bóng ẩn nhìn thẳng xuống, render layer "CloudShadow"
// vào RenderTexture rồi gửi texture đó vào shader toàn cục.
using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class CloudShadowSystem : MonoBehaviour
{
    public static CloudShadowSystem Instance { get; private set; }

    [Header("Shadow Map")]
    [Tooltip("Độ phân giải texture bóng (512 hoặc 1024 là đủ)")]
    public int resolution = 1024;

    [Tooltip("Diện tích thế giới (world units) mà shadow map bao phủ")]
    public float worldSize = 200f;

    [Tooltip("Chiều cao đặt camera bóng, phải cao hơn mọi cloud")]
    public float cameraHeight = 500f;

    // ── private ──────────────────────────────────────────────────────────
    Camera        _shadowCam;
    RenderTexture _rt;

    static readonly int s_Tex     = Shader.PropertyToID("_CloudShadowTex");
    static readonly int s_Center  = Shader.PropertyToID("_CloudShadowAreaCenter");
    static readonly int s_Extents = Shader.PropertyToID("_CloudShadowAreaExtents");

    // ── lifecycle ─────────────────────────────────────────────────────────
    void OnEnable()
    {
        Instance = this;
        CreateShadowCamera();
        CreateRT();
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;

        if (_shadowCam != null)
        {
            if (Application.isPlaying) Destroy(_shadowCam.gameObject);
            else                       DestroyImmediate(_shadowCam.gameObject);
        }

        _rt?.Release();
        _rt = null;
    }

    // ── setup ─────────────────────────────────────────────────────────────
    void CreateShadowCamera()
    {
        var go = new GameObject("__CloudShadowCamera")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // nhìn thẳng xuống

        _shadowCam = go.AddComponent<Camera>();
        _shadowCam.clearFlags       = CameraClearFlags.SolidColor;
        _shadowCam.backgroundColor  = Color.black;
        _shadowCam.orthographic     = true;
        _shadowCam.orthographicSize = worldSize * 0.5f;
        _shadowCam.nearClipPlane    = 1f;
        _shadowCam.farClipPlane     = cameraHeight * 2f;
        _shadowCam.cullingMask      = LayerMask.GetMask("CloudShadow");
        _shadowCam.allowHDR         = false;
        _shadowCam.allowMSAA        = false;

        // URP: tắt những thứ không cần thiết cho camera bóng này
        var urpData = go.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderPostProcessing = false;
        urpData.renderShadows        = false;
        urpData.requiresColorTexture = false;
        urpData.requiresDepthTexture = false;
        urpData.antialiasing         = AntialiasingMode.None;
    }

    void CreateRT()
    {
        _rt?.Release();
        _rt = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.R8)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
            name       = "CloudShadowRT"
        };
        _rt.Create();
        _shadowCam.targetTexture = _rt;
    }

    // ── per-frame ─────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (_shadowCam == null || _rt == null) return;

        // camera bóng đi theo main camera theo trục XZ
        if (Camera.main != null)
        {
            Vector3 p = Camera.main.transform.position;
            _shadowCam.transform.position = new Vector3(p.x, cameraHeight, p.z);
        }

        // cập nhật shader globals để GrassShader biết vị trí và kích thước shadow map
        float   half   = worldSize * 0.5f;
        Vector3 center = _shadowCam.transform.position;

        Shader.SetGlobalTexture(s_Tex,    _rt);
        Shader.SetGlobalVector(s_Center,  new Vector4(center.x, 0f, center.z, 0f));
        Shader.SetGlobalVector(s_Extents, new Vector4(half,     0f, half,     0f));
    }
}
