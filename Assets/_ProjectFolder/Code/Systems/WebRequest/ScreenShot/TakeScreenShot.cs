using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.WebRequest
{
    public class TakeScreenShot : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField, Range(1, 4)] private int superSize = 1;
        [SerializeField] private GameObject[] _hideElements;
        [SerializeField] private UnityEvent<Texture2D> _onScreenCaptured;
        
        private bool _isCapturing;

        private IEnumerator CaptureCoroutine()
        {
            if (_isCapturing) yield break;

            _isCapturing = true;
            SetVisibility(false);
            yield return new WaitForEndOfFrame();
            
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture(superSize);
            _onScreenCaptured.Invoke(tex);
        }
        private void SetVisibility(bool value)
        {
            foreach (var element in _hideElements)
                element.SetActive(value);
        }

        [ContextMenu("Tomar Captura")]
        public void CaptureScreenshot() => StartCoroutine(CaptureCoroutine());
        public void OnContinue()
        {
            SetVisibility(true);
            _isCapturing = false;
        }
    }
}