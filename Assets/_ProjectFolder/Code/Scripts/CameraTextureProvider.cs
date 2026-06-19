using UnityEngine;

public class CameraTextureProvider : MonoBehaviour
{
    [Header("Configuración Webcam")]
    [SerializeField] private Vector2Int requestedResolution = new Vector2Int(1280, 720);
    [SerializeField] private int requestedFPS = 30;

    private WebCamTexture _webCamTexture;
    
    public Texture VideoTexture => _webCamTexture;
    public bool IsPlaying => _webCamTexture != null && _webCamTexture.isPlaying;
    public Vector2Int Resolution => _webCamTexture != null ? new Vector2Int(_webCamTexture.width, _webCamTexture.height) : Vector2Int.zero;

    private void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        
        if (devices.Length == 0)
        {
            Debug.LogError("[CameraTextureProvider] No se encontraron webcams.");
            return;
        }

        string deviceName = devices[0].name;

        _webCamTexture = new WebCamTexture(deviceName, requestedResolution.x, requestedResolution.y, requestedFPS);
        _webCamTexture.Play();
    }
    private void OnDestroy()
    {
        if (_webCamTexture != null)
            _webCamTexture.Stop();
    }
}