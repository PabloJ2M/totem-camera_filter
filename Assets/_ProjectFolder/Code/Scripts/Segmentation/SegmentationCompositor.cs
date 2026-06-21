using UnityEngine;

namespace Mediapipe.Unity
{
    using Sample;

    public class SegmentationCompositor : MonoBehaviour
    {
        [SerializeField] private ImageSegmentationResult _resultMask;
        [SerializeField] private Material _material;

        private void LateUpdate()
        {
            var _source = ImageSourceProvider.ImageSource;
            
            if (_source == null) return;
            if (!_source.isPrepared) return;

            var texture = _source.GetCurrentTexture();

            _material.SetTexture("_WebcamTex", texture);
            _material.SetTexture("_MaskTex", _resultMask.MaskTexture);
        }
    }
}