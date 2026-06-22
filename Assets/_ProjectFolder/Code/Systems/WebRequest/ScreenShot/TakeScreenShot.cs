using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.WebRequest
{
    public class TakeScreenShot : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private string subfolder = "Screenshots";
        [SerializeField] private string filePrefix = "screenshot";
        [SerializeField] private GameObject[] _hideElements;

        [SerializeField, Range(1, 4)] private int superSize = 1;
        [SerializeField] private UnityEvent<string> _onScreenCaptured;

        private bool _isCapturing;
        private string SaveDirectory => Path.Combine(Application.persistentDataPath, subfolder);

        private void Awake()
        {
            if (!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);
        }

        private IEnumerator CaptureCoroutine()
        {
            if (_isCapturing) yield break;

            _isCapturing = true;
            SetVisibility(false);
            yield return new WaitForEndOfFrame();
            string filePath = Path.Combine(SaveDirectory, $"{filePrefix}.png");

            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture(superSize);
            byte[] bytes = tex.EncodeToPNG();
            Destroy(tex);

            File.WriteAllBytes(filePath, bytes);
            print($"[ScreenshotCapture] Guardado en: {filePath}");
            _onScreenCaptured.Invoke(filePath);

            SetVisibility(true);
            _isCapturing = false;
        }
        private void SetVisibility(bool value)
        {
            foreach (var element in _hideElements)
                element.SetActive(value);
        }

        [ContextMenu("Tomar Captura")]
        public void CaptureScreenshot() => StartCoroutine(CaptureCoroutine());

        [ContextMenu("Abrir carpeta de capturas")]
        private void OpenSaveDirectory() => Process.Start(SaveDirectory);

        [ContextMenu("Mostrar ruta en consola")]
        private void PrintSaveDirectory() => print($"[ScreenshotCapture] Ruta: {SaveDirectory}");
    }
}