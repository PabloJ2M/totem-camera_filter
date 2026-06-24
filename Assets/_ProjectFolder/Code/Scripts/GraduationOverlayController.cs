using UnityEngine;
using Rect = UnityEngine.Rect;
using Mediapipe.Tasks.Vision.PoseLandmarker;

public class GraduationOverlayController : MonoBehaviour
{
    [SerializeField] private RectTransform referenceRect;

    [SerializeField] private RectTransform birreteRect;
    public static float birreteVerticalOffset = -0.64f;
    public static float birreteHorizontalOffset = 0f;
    public static float birreteScaleMultiplier = 2.86f;
    public static float birreteMinVisibility = 0.05f;

    public static bool birreteFlipX = false;
    public static bool birreteFlipY = true;

	public void BirreteFlipX(bool value) => birreteFlipX = value;
	public void SetBirreteYOffset(float value) => birreteVerticalOffset = value;
    public void SetBirreteXOffset(float value) => birreteHorizontalOffset = value;
    public void SetBirreteSize(float value) => birreteScaleMultiplier = value;

    [SerializeField] private RectTransform togaRect;
    [SerializeField] private float togaSmoothSpeed = 0.15f;

    public static float togaVerticalOffset = 1.05f;
    public static float togaHorizontalOffset = 0f;
    public static float togaScaleMultiplier = 4f;
    public static float togaMinVisibility = 0.05f;
    public bool  togaFollowRotation = false;

    private bool _togaInitialized = false;
    private Vector2 _togaSmoothedPosition;
    private Vector2 _togaSmoothedSize;

    public void SetTogaVerticalOffset(float value) => togaVerticalOffset = value;
    public void SetTogaHorizontalOffset(float value) => togaHorizontalOffset = -value;
    public void SetTogaSize(float value) => togaScaleMultiplier = value;

    [Header("Ajuste de coordenadas")]
    public bool flipX = false;

    private const int LEFT_EAR = 7;
    private const int RIGHT_EAR = 8;
    private const int LEFT_SHOULDER = 11;
    private const int RIGHT_SHOULDER = 12;
    private const int LEFT_HIP = 23;
    private const int RIGHT_HIP = 24;

    PoseLandmarkerResult? _pendingResult;
    readonly object _lock = new object();

    void Update()
    {
        PoseLandmarkerResult? result;
        lock (_lock)
        {
            result = _pendingResult;
            _pendingResult = null;
        }
        if (result != null)
            ApplyResult(result.Value);
    }

    public void UpdateFromResult(PoseLandmarkerResult result)
    {
        lock (_lock) { _pendingResult = result; }
    }

    void ApplyResult(PoseLandmarkerResult result)
    {
        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
        {
            SetVisible(birreteRect, false);
            SetVisible(togaRect, false);
            return;
        }

        var landmarks = result.poseLandmarks[0].landmarks;
        if (landmarks == null || landmarks.Count < 25)
        {
            SetVisible(birreteRect, false);
            SetVisible(togaRect, false);
            return;
        }

        Rect rect = referenceRect != null
            ? referenceRect.rect
            : new Rect(-Screen.width / 2f, -Screen.height / 2f, Screen.width, Screen.height);

        // --- Birrete ---
        var lEar = landmarks[LEFT_EAR];
        var rEar = landmarks[RIGHT_EAR];
        bool showBirrete = lEar.visibility >= birreteMinVisibility && rEar.visibility >= birreteMinVisibility;

        SetVisible(birreteRect, showBirrete);
        if (showBirrete && birreteRect != null)
            PlaceOverlay(birreteRect,
                ToLocal(lEar.x, lEar.y, rect),
                ToLocal(rEar.x, rEar.y, rect),
                birreteVerticalOffset, birreteHorizontalOffset,
                birreteScaleMultiplier, birreteFlipX, birreteFlipY);

        // --- Toga ---
        var lSh = landmarks[LEFT_SHOULDER];
        var rSh = landmarks[RIGHT_SHOULDER];
        var lHp = landmarks[LEFT_HIP];
        var rHp = landmarks[RIGHT_HIP];
        bool showToga = lSh.visibility >= togaMinVisibility && rSh.visibility >= togaMinVisibility && lHp.visibility >= togaMinVisibility && rHp.visibility >= togaMinVisibility;

        SetVisible(togaRect, showToga);
        if (showToga && togaRect != null)
        {
            Vector2 lShoulder = ToLocal(lSh.x, lSh.y, rect);
            Vector2 rShoulder = ToLocal(rSh.x, rSh.y, rect);
            Vector2 lHip = ToLocal(lHp.x, lHp.y, rect);
            Vector2 rHip = ToLocal(rHp.x, rHp.y, rect);

            float shoulderDist = Vector2.Distance(lShoulder, rShoulder);
            float shoulderAngle = Mathf.Atan2(rShoulder.y - lShoulder.y, rShoulder.x - lShoulder.x) * Mathf.Rad2Deg;

            Vector2 topMid = (lShoulder + rShoulder) * 0.5f;
            Vector2 bottomMid = (lHip + rHip) * 0.5f;
            Vector2 right2mid = (rShoulder - lShoulder).normalized;

            Vector2 anchor = Vector2.LerpUnclamped(topMid, bottomMid, togaVerticalOffset) + right2mid * shoulderDist * togaHorizontalOffset;
            Vector2 targetSize = Vector2.one * shoulderDist * togaScaleMultiplier;

            if (!_togaInitialized)
            {
                _togaSmoothedPosition = anchor;
                _togaSmoothedSize = targetSize;
                _togaInitialized = true;
            }

            float t = 1f - Mathf.Pow(1f - togaSmoothSpeed, Time.deltaTime * 60f);
            _togaSmoothedPosition = Vector2.Lerp(_togaSmoothedPosition, anchor, t);
            _togaSmoothedSize = Vector2.Lerp(_togaSmoothedSize, targetSize, t);

            togaRect.anchoredPosition = _togaSmoothedPosition;
            togaRect.sizeDelta = _togaSmoothedSize;
            togaRect.localRotation = togaFollowRotation ? Quaternion.Euler(0f, 0f, -shoulderAngle) : Quaternion.identity;
        }
    }

    private Vector2 ToLocal(float nx, float ny, Rect rect)
    {
        float x = flipX ? (0.5f - nx) : (nx - 0.5f);
        float y = 0.5f - ny;
        return new Vector2(x * rect.width, y * rect.height);
    }

    void PlaceOverlay(RectTransform rt, Vector2 left, Vector2 right,
                      float verticalOffset, float horizontalOffset,
                      float scaleMultiplier, bool flipX = false, bool flipY = false)
    {
        Vector2 mid = (left + right) * 0.5f;
        float dist = Vector2.Distance(left, right);
        float angle = Mathf.Atan2(right.y - left.y, right.x - left.x) * Mathf.Rad2Deg;
        float rad = angle * Mathf.Deg2Rad;
        Vector2 up = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
        Vector2 side = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        rt.anchoredPosition = mid + up * dist * verticalOffset + side * dist * horizontalOffset;
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        rt.sizeDelta = Vector2.one * dist * scaleMultiplier;
        rt.localScale = new Vector3(flipX ? -1f : 1f, flipY ? -1f : 1f, 1f);
    }

    private void SetVisible(RectTransform rt, bool visible)
    {
        if (rt != null && rt.gameObject.activeSelf != visible)
            rt.gameObject.SetActive(visible);
    }
}