using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;
using Rect = UnityEngine.Rect;

/// <summary>
/// Posiciona el birrete y la toga usando los landmarks de MediaPipe PoseLandmarker.
/// La composición de fondo (máscara + webcam) la maneja SegmentationCompositor por separado.
/// Thread-safe: UpdateFromResult() puede llamarse desde cualquier hilo.
/// </summary>
public class GraduationOverlayController : MonoBehaviour
{
    [Header("Referencia de área")]
    [Tooltip("RectTransform del RawImage que cubre toda la composición final")]
    public RectTransform referenceRect;

    [Header("Birrete")]
    public RectTransform birreteRect;
    public float birreteVerticalOffset  = 0.9f;
    public float birreteScaleMultiplier = 2.4f;
    public float birreteMinVisibility   = 0.5f;
    [Tooltip("Voltear el birrete horizontalmente")]
    public bool  birreteFlipX          = false;
    [Tooltip("Voltear el birrete verticalmente (equivale a sumar 180° a la rotación)")]
    public bool  birreteFlipY          = false;

    [Header("Toga")]
    public RectTransform togaRect;
    public float togaVerticalOffset     = 0.1f;
    public float togaScaleMultiplier    = 1.2f;
    public float togaMinVisibility      = 0.5f;
    public bool  togaFollowRotation     = false;

    [Header("Ajuste de coordenadas")]
    public bool flipX = false;

    const int LEFT_EAR       = 7;
    const int RIGHT_EAR      = 8;
    const int LEFT_SHOULDER  = 11;
    const int RIGHT_SHOULDER = 12;
    const int LEFT_HIP       = 23;
    const int RIGHT_HIP      = 24;

    // Thread-safe: el callback de LIVE_STREAM llega desde hilo secundario
    PoseLandmarkerResult? _pendingResult;
    readonly object       _lock = new object();

    void Update()
    {
        PoseLandmarkerResult? result;
        lock (_lock)
        {
            result         = _pendingResult;
            _pendingResult = null;
        }
        if (result != null)
            ApplyResult(result.Value);
    }

    /// <summary>Seguro para llamar desde cualquier hilo.</summary>
    public void UpdateFromResult(PoseLandmarkerResult result)
    {
        lock (_lock) { _pendingResult = result; }
    }

    void ApplyResult(PoseLandmarkerResult result)
    {
        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
        {
            SetVisible(birreteRect, false);
            SetVisible(togaRect,    false);
            return;
        }

        var landmarks = result.poseLandmarks[0].landmarks;
        if (landmarks == null || landmarks.Count < 25)
        {
            SetVisible(birreteRect, false);
            SetVisible(togaRect,    false);
            return;
        }

        Rect rect = referenceRect != null
            ? referenceRect.rect
            : new Rect(-Screen.width / 2f, -Screen.height / 2f, Screen.width, Screen.height);

        // --- Birrete ---
        var lEar = landmarks[LEFT_EAR];
        var rEar = landmarks[RIGHT_EAR];
        bool showBirrete = lEar.visibility >= birreteMinVisibility &&
                           rEar.visibility >= birreteMinVisibility;
        SetVisible(birreteRect, showBirrete);
        if (showBirrete && birreteRect != null)
            PlaceOverlay(birreteRect,
                ToLocal(lEar.x, lEar.y, rect),
                ToLocal(rEar.x, rEar.y, rect),
                birreteVerticalOffset, birreteScaleMultiplier, birreteFlipX, birreteFlipY);

        // --- Toga ---
        var lSh = landmarks[LEFT_SHOULDER];
        var rSh = landmarks[RIGHT_SHOULDER];
        var lHp = landmarks[LEFT_HIP];
        var rHp = landmarks[RIGHT_HIP];
        bool showToga = lSh.visibility >= togaMinVisibility &&
                        rSh.visibility >= togaMinVisibility &&
                        lHp.visibility >= togaMinVisibility &&
                        rHp.visibility >= togaMinVisibility;
        SetVisible(togaRect, showToga);
        if (showToga && togaRect != null)
        {
            Vector2 lShoulder = ToLocal(lSh.x, lSh.y, rect);
            Vector2 rShoulder = ToLocal(rSh.x, rSh.y, rect);
            Vector2 lHip      = ToLocal(lHp.x, lHp.y, rect);
            Vector2 rHip      = ToLocal(rHp.x, rHp.y, rect);

            Vector2 topMid    = (lShoulder + rShoulder) * 0.5f;
            Vector2 bottomMid = (lHip + rHip) * 0.5f;
            Vector2 anchor    = Vector2.LerpUnclamped(topMid, bottomMid, togaVerticalOffset);

            float shoulderDist  = Vector2.Distance(lShoulder, rShoulder);
            float shoulderAngle = Mathf.Atan2(rShoulder.y - lShoulder.y,
                                              rShoulder.x - lShoulder.x) * Mathf.Rad2Deg;

            togaRect.anchoredPosition = anchor;
            togaRect.sizeDelta        = Vector2.one * shoulderDist * togaScaleMultiplier;
            togaRect.localRotation    = togaFollowRotation
                ? Quaternion.Euler(0f, 0f, -shoulderAngle)
                : Quaternion.identity;
        }
    }

    Vector2 ToLocal(float nx, float ny, Rect rect)
    {
        float x = flipX ? (0.5f - nx) : (nx - 0.5f);
        float y = 0.5f - ny;
        return new Vector2(x * rect.width, y * rect.height);
    }

    void PlaceOverlay(RectTransform rt, Vector2 left, Vector2 right,
                      float verticalOffset, float scaleMultiplier,
                      bool flipX = false, bool flipY = false)
    {
        Vector2 mid = (left + right) * 0.5f;
        float dist  = Vector2.Distance(left, right);
        float angle = Mathf.Atan2(right.y - left.y, right.x - left.x) * Mathf.Rad2Deg;
        float rad   = angle * Mathf.Deg2Rad;
        Vector2 up  = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));

        rt.anchoredPosition = mid + up * dist * verticalOffset;
        rt.localRotation    = Quaternion.Euler(0f, 0f, angle);
        rt.sizeDelta        = Vector2.one * dist * scaleMultiplier;

        // flipX/flipY mediante localScale: no interfiere con rotación ni sizeDelta
        rt.localScale = new Vector3(flipX ? -1f : 1f, flipY ? -1f : 1f, 1f);
    }

    void SetVisible(RectTransform rt, bool visible)
    {
        if (rt != null && rt.gameObject.activeSelf != visible)
            rt.gameObject.SetActive(visible);
    }
}