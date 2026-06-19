using Mediapipe;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;
using UnityEngine.UI;
using Rect = UnityEngine.Rect;

public class GraduationOverlayController : MonoBehaviour
{
    [Header("Composición de fondo")]
    public RawImage   compositeOutput;
    public Material   compositeMaterial;
    public Texture    backgroundTexture;
    public Vector2Int maskResolution = new Vector2Int(256, 256);

    [Header("Birrete")]
    public RectTransform birreteRect;
    public float birreteVerticalOffset  = 0.9f;
    public float birreteScaleMultiplier = 2.4f;
    public float birreteMinVisibility   = 0.5f;

    [Header("Toga")]
    public RectTransform togaRect;
    public float togaVerticalOffset     = 0.1f;
    public float togaScaleMultiplier    = 1.2f;
    public float togaMinVisibility      = 0.5f;
    public bool  togaFollowRotation     = false;

    [Header("Debug")]
    public bool flipX = false;

    const int LEFT_EAR       = 7;
    const int RIGHT_EAR      = 8;
    const int LEFT_SHOULDER  = 11;
    const int RIGHT_SHOULDER = 12;
    const int LEFT_HIP       = 23;
    const int RIGHT_HIP      = 24;

    RenderTexture _maskRT;
    Texture2D     _maskTex2D;

    // ---- Thread-safe buffer ----
    // Los bytes de la máscara se leen en el callback (cualquier hilo, antes
    // de que DisposeAllMasks los destruya) y se suben a la textura en Update()
    // (hilo principal de Unity).
    PoseLandmarkerResult? _pendingResult;
    Texture               _pendingSourceTex;
    float[]               _pendingMaskData;
    int                   _pendingMaskW, _pendingMaskH;
    readonly object       _lock = new object();

    // ------------------------------------------------------------------ //
    // Unity lifecycle
    // ------------------------------------------------------------------ //

    void Start()
    {
        _maskRT   = new RenderTexture(maskResolution.x, maskResolution.y, 0, RenderTextureFormat.RFloat);
        _maskTex2D = new Texture2D(maskResolution.x, maskResolution.y, TextureFormat.RFloat, false);

        if (compositeMaterial != null)
        {
            if (backgroundTexture != null)
                compositeMaterial.SetTexture("_BackgroundTex", backgroundTexture);
            compositeMaterial.SetTexture("_MaskTex", _maskRT);
        }

        if (compositeOutput != null)
            compositeOutput.material = compositeMaterial;
    }

    void Update()
    {
        PoseLandmarkerResult? result;
        Texture sourceTex;
        float[] maskData;
        int     maskW, maskH;

        lock (_lock)
        {
            result            = _pendingResult;
            sourceTex         = _pendingSourceTex;
            maskData          = _pendingMaskData;
            maskW             = _pendingMaskW;
            maskH             = _pendingMaskH;
            _pendingResult    = null;
            _pendingSourceTex = null;
            _pendingMaskData  = null;
        }

        if (result == null) return;

        if (sourceTex != null && compositeMaterial != null)
            compositeMaterial.SetTexture("_WebcamTex", sourceTex);

        // Subir bytes de máscara a GPU (hilo principal)
        if (maskData != null)
            UploadMask(maskData, maskW, maskH);

        ApplyResult(result.Value);
    }

    void OnDestroy()
    {
        if (_maskRT    != null) _maskRT.Release();
        if (_maskTex2D != null) Destroy(_maskTex2D);
    }

    // ------------------------------------------------------------------ //
    // API pública — seguros desde cualquier hilo
    // ------------------------------------------------------------------ //

    public void SetSourceTexture(Texture tex)
    {
        lock (_lock) { _pendingSourceTex = tex; }
    }

    /// <summary>
    /// Lee los bytes de la máscara INMEDIATAMENTE (antes de que el runner
    /// llame a DisposeAllMasks) y encola el resultado para Update().
    /// </summary>
    public void UpdateFromResult(PoseLandmarkerResult result)
    {
        float[] maskData = null;
        int     maskW = 0, maskH = 0;

        if (result.segmentationMasks != null && result.segmentationMasks.Count > 0)
            ReadMaskBytes(result.segmentationMasks[0], out maskData, out maskW, out maskH);

        lock (_lock)
        {
            _pendingResult   = result;
            _pendingMaskData = maskData;
            _pendingMaskW    = maskW;
            _pendingMaskH    = maskH;
        }
    }

    // ------------------------------------------------------------------ //
    // Máscara — lectura (cualquier hilo) y subida a GPU (hilo principal)
    // ------------------------------------------------------------------ //

    void ReadMaskBytes(Mediapipe.Image mask, out float[] data, out int w, out int h)
    {
        data = null; w = 0; h = 0;
        try
        {
            if (!mask.ConvertToCpu())
            {
                Debug.LogWarning("[GraduationOverlayController] ConvertToCpu falló.");
                return;
            }

            w = mask.Width();
            h = mask.Height();
            int step = mask.Step(); 
            
            using var pixelLock = new Mediapipe.PixelWriteLock(mask);
            var ptr = pixelLock.Pixels();

            // Detectar el formato real analizando cuántos bytes ocupa cada píxel
            int bytesPerPixel = step / w;
            int totalPixels = w * h;
            data = new float[totalPixels];

            if (bytesPerPixel >= 4)
            {
                // CASO A: MediaPipe está enviando Floats (VEC32F1) o canales de 4 bytes
                // Comprobamos si realmente son floats nativos
                int floatsPerRow = step / sizeof(float);
                var rawFloats = new float[floatsPerRow * h];
                System.Runtime.InteropServices.Marshal.Copy(ptr, rawFloats, 0, rawFloats.Length);

                // Copiar eliminando el padding de fila si existiera
                for (int row = 0; row < h; row++)
                {
                    for (int col = 0; col < w; col++)
                    {
                        float val = rawFloats[row * floatsPerRow + col];
                        // Si por algún motivo vienen bytes camuflados en un entero de 32 bits, 
                        // o el float es mayor a 1.0, lo normalizamos
                        data[row * w + col] = val > 1.0f ? val / 255f : val;
                    }
                }
            }
            else
            {
                // CASO B: MediaPipe está enviando canales de 1 byte (GRAY8 / Máscara de baja resolución)
                var rawBytes = new byte[step * h];
                System.Runtime.InteropServices.Marshal.Copy(ptr, rawBytes, 0, rawBytes.Length);

                for (int row = 0; row < h; row++)
                {
                    for (int col = 0; col < w; col++)
                    {
                        // Convertimos el byte (0-255) a un float normalizado (0.0 - 1.0) para el shader
                        data[row * w + col] = rawBytes[row * step + col] / 255f;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[GraduationOverlayController] ReadMaskBytes Corregido Falló: {e.Message}");
            data = null;
        }
    }

    void UploadMask(float[] data, int w, int h)
    {
        if (_maskTex2D == null || _maskTex2D.width != w || _maskTex2D.height != h)
        {
            if (_maskTex2D != null) Destroy(_maskTex2D);
            _maskTex2D = new Texture2D(w, h, TextureFormat.RFloat, false);
        }

        var raw = _maskTex2D.GetRawTextureData<float>();
        for (int i = 0; i < data.Length && i < raw.Length; i++)
            raw[i] = data[i];

        _maskTex2D.Apply(false);
        Graphics.Blit(_maskTex2D, _maskRT);
    }

    // ------------------------------------------------------------------ //
    // Lógica de overlays — solo desde Update() (hilo principal)
    // ------------------------------------------------------------------ //

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

        Rect rect = GetReferenceRect();

        // Birrete
        var lEar = landmarks[LEFT_EAR];
        var rEar = landmarks[RIGHT_EAR];
        bool showBirrete = lEar.visibility >= birreteMinVisibility &&
                           rEar.visibility >= birreteMinVisibility;
        SetVisible(birreteRect, showBirrete);
        if (showBirrete && birreteRect != null)
            PlaceOverlay(birreteRect,
                         ToLocal(lEar.x, lEar.y, rect),
                         ToLocal(rEar.x, rEar.y, rect),
                         birreteVerticalOffset, birreteScaleMultiplier);

        // Toga
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
            Vector2 anchor    = Vector2.Lerp(topMid, bottomMid, togaVerticalOffset);

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

    // ------------------------------------------------------------------ //
    // Helpers
    // ------------------------------------------------------------------ //

    Rect GetReferenceRect()
    {
        if (compositeOutput != null) return compositeOutput.rectTransform.rect;
        return new Rect(-Screen.width / 2f, -Screen.height / 2f, Screen.width, Screen.height);
    }

    Vector2 ToLocal(float nx, float ny, Rect rect)
    {
        float x = flipX ? (0.5f - nx) : (nx - 0.5f);
        float y = 0.5f - ny;
        return new Vector2(x * rect.width, y * rect.height);
    }

    void PlaceOverlay(RectTransform rt, Vector2 left, Vector2 right,
                      float verticalOffset, float scaleMultiplier)
    {
        Vector2 mid  = (left + right) * 0.5f;
        float dist   = Vector2.Distance(left, right);
        float angle  = Mathf.Atan2(right.y - left.y, right.x - left.x) * Mathf.Rad2Deg;
        float rad    = angle * Mathf.Deg2Rad;
        Vector2 up   = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));

        rt.anchoredPosition = mid + up * dist * verticalOffset;
        rt.localRotation    = Quaternion.Euler(0f, 0f, -angle);
        rt.sizeDelta        = Vector2.one * dist * scaleMultiplier;
    }

    void SetVisible(RectTransform rt, bool visible)
    {
        if (rt != null && rt.gameObject.activeSelf != visible)
            rt.gameObject.SetActive(visible);
    }
}