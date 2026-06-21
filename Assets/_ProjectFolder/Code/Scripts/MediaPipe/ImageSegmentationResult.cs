using Mediapipe.Tasks.Vision.ImageSegmenter;
using UnityEngine;

namespace Mediapipe.Unity
{
    public class ImageSegmentationResult : AnnotationController<MaskTextureAnnotation>
    {
        private int _maskWidth;
        private int _maskHeight;

        private readonly object _currentTargetLock = new object();
        private ImageSegmenterResult _currentTarget;
        private int _maskIndex = 0;

        public RenderTexture MaskTexture => annotation != null ? annotation.MaskTexture : null;

        public void InitScreen(int maskWidth, int maskHeight)
        {
            _maskWidth = maskWidth;
            _maskHeight = maskHeight;
            annotation.Init(_maskWidth, _maskHeight);
        }

        public void DrawNow(ImageSegmenterResult target)
        {
            _currentTarget = target;
            ReadCurrentMasks();
            SyncNow();
        }
        public void DrawLater(ImageSegmenterResult target) => UpdateCurrentTarget(target);
        public void SelectMask(int maskIndex) => _maskIndex = maskIndex;

        protected void UpdateCurrentTarget(ImageSegmenterResult newTarget)
        {
            lock (_currentTargetLock)
            {
                _currentTarget = newTarget;
                ReadCurrentMasks();
                isStale = true;
            }
        }
        protected override void SyncNow()
        {
            lock (_currentTargetLock)
            {
                isStale = false;
                annotation.Draw();
            }
        }

        private void ReadCurrentMasks()
        {
            if (annotation != null)
            {
                annotation.ReadMaskData(_currentTarget, _maskIndex);
            }
        }
    }
}