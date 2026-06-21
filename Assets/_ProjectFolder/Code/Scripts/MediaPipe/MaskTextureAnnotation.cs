using System.Runtime.InteropServices;
using Mediapipe.Tasks.Vision.ImageSegmenter;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mediapipe.Unity
{
    public class MaskTextureAnnotation : HierarchicalAnnotation
    {
        [SerializeField, Range(0, 1)] private float _threshold = 0.4f;
        [SerializeField] private RawImage _debug;

        private int _width;
        private int _height;
        private float[] _maskArray;

        private RenderTexture _maskRT;
        private Texture2D _maskTex2D;

        public RenderTexture MaskTexture => _maskRT;

        public void Init(int width, int height)
        {
            _width = width;
            _height = height;

            if (_maskRT != null) _maskRT.Release();
            if (_maskTex2D != null) Destroy(_maskTex2D);

            _maskRT = new RenderTexture(_width, _height, 0, RenderTextureFormat.R8);
            _maskRT.filterMode = FilterMode.Bilinear;
            _maskRT.Create();

            _maskTex2D = new Texture2D(_width, _height, TextureFormat.R8, false);
            _maskArray = new float[_width * _height];
        }
        public void ReadMaskData(ImageSegmenterResult currentTarget, int maskIndex)
        {
            if (currentTarget.confidenceMasks == null || currentTarget.confidenceMasks.Count <= maskIndex) return;

            var mask = currentTarget.confidenceMasks[maskIndex];

            if (mask.Width() != _width || mask.Height() != _height)
            {
                Init(mask.Width(), mask.Height());
            }

            if (mask.ConvertToCpu())
            {
                using (var pixelLock = new PixelWriteLock(mask))
                {
                    var ptr = pixelLock.Pixels();
                    Marshal.Copy(ptr, _maskArray, 0, _maskArray.Length);
                }
            }
        }
        public void Draw()
        {
            if (_maskArray == null || _maskArray.Length == 0) return;

            NativeArray<byte> pixelData = _maskTex2D.GetRawTextureData<byte>();
            for (int i = 0; i < _maskArray.Length; i++)
            {
                pixelData[i] = (byte)(_maskArray[i] > _threshold ? 255 : 0);
            }

            _maskTex2D.Apply();
            Graphics.Blit(_maskTex2D, _maskRT);

            if (_debug)
                _debug.texture = MaskTexture;
        }

        private void OnDestroy()
        {
            if (_maskRT != null) _maskRT.Release();
            if (_maskTex2D != null) Destroy(_maskTex2D);
        }
    }
}