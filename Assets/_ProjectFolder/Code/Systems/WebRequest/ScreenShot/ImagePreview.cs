using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

public class ImagePreview : MonoBehaviour
{
    [SerializeReference] private GameObject _preview;
    [SerializeReference] private Image _image;
    [SerializeReference] private CanvasGroup _canvasGroup;

    [SerializeField] private string subfolder = "Screenshots";
    [SerializeField] private string filePrefix = "screenshot";

    [SerializeField] private UnityEvent<string> _onScreenCaptured;

    private string SaveDirectory => Path.Combine(Application.persistentDataPath, subfolder);

    private Texture2D _currentTexture;

    private void Awake()
    {
        if (!Directory.Exists(SaveDirectory))
            Directory.CreateDirectory(SaveDirectory);
    }
    public void SetImage(Texture2D texture)
    {
        _currentTexture = texture;
        _canvasGroup.alpha = 0f;
        _image.color = Color.white;
        _preview.SetActive(true);
        StartCoroutine(Delay());
    }
    public void Continue()
    {
        byte[] bytes = _currentTexture.EncodeToPNG();
        string filePath = Path.Combine(SaveDirectory, $"{filePrefix}.png");
        
        Close();

        File.WriteAllBytes(filePath, bytes);
        print($"[ScreenshotCapture] Guardado en: {filePath}");

        _onScreenCaptured.Invoke(filePath);
    }
    public void Close()
    {
        _preview.SetActive(false);
        _canvasGroup.alpha = 0f;

        Destroy(_currentTexture);
    }

    private IEnumerator Delay()
    {
        yield return new WaitForSeconds(0.5f);
        _image.color = new Color(1f, 1f, 1f, 0f); ;

        yield return new WaitForSeconds(1.5f);
        _canvasGroup.alpha = 1f;
    }

    [ContextMenu("Abrir carpeta de capturas")]
    private void OpenSaveDirectory() => Process.Start(SaveDirectory);

    [ContextMenu("Mostrar ruta en consola")]
    private void PrintSaveDirectory() => print($"[ScreenshotCapture] Ruta: {SaveDirectory}");
}