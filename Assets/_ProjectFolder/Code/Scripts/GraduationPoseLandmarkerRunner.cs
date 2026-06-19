using System.Collections;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
    /// <summary>
    /// Variante de PoseLandmarkerRunner con segmentación activada y
    /// sin dependencia de BodyPix/Sentis.
    /// Habilita OutputSegmentationMasks y pasa el resultado a
    /// GraduationOverlayController antes de destruir las máscaras.
    /// </summary>
    public class GraduationPoseLandmarkerRunner : VisionTaskApiRunner<PoseLandmarker>
    {
        [SerializeField] private PoseLandmarkerResultAnnotationController _annotationController;
        [SerializeField] private GraduationOverlayController _overlayController;

        private Experimental.TextureFramePool _textureFramePool;
        private Mediapipe.Unity.ImageSource   _imageSource;

        public readonly PoseLandmarkDetectionConfig config = new PoseLandmarkDetectionConfig();

        public override void Stop()
        {
            base.Stop();
            _textureFramePool?.Dispose();
            _textureFramePool = null;
        }

        protected override IEnumerator Run()
        {
            // Activar la máscara de segmentación
            config.OutputSegmentationMasks = true;

            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            var options = config.GetPoseLandmarkerOptions(
                config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM
                    ? OnDetectionOutput : null);

            taskApi = PoseLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
            _imageSource = ImageSourceProvider.ImageSource;
            var imageSource = _imageSource;

            yield return imageSource.Play();

            if (!imageSource.isPrepared)
            {
                Logger.LogError(TAG, "Failed to start ImageSource, exiting...");
                yield break;
            }

            _textureFramePool = new Experimental.TextureFramePool(
                imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);

            screen.Initialize(imageSource);
            SetupAnnotationController(_annotationController, imageSource);
            _annotationController.InitScreen(imageSource.textureWidth, imageSource.textureHeight);

            var transformationOptions  = imageSource.GetTransformationOptions();
            var flipHorizontally       = transformationOptions.flipHorizontally;
            var flipVertically         = transformationOptions.flipVertically;
            var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);

            AsyncGPUReadbackRequest req         = default;
            var waitUntilReqDone  = new WaitUntil(() => req.done);
            var waitForEndOfFrame = new WaitForEndOfFrame();
            var result = PoseLandmarkerResult.Alloc(options.numPoses, options.outputSegmentationMasks);

            var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3
                                 && GpuManager.GpuResources != null;
            using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

            while (true)
            {
                if (isPaused)
                    yield return new WaitWhile(() => isPaused);

                if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                Image image;
                switch (config.ImageReadMode)
                {
                    case ImageReadMode.GPU:
                        if (!canUseGpuImage)
                            throw new System.Exception("ImageReadMode.GPU is not supported");
                        textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        image = textureFrame.BuildGPUImage(glContext);
                        yield return waitForEndOfFrame;
                        break;

                    case ImageReadMode.CPU:
                        yield return waitForEndOfFrame;
                        textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        image = textureFrame.BuildCPUImage();
                        textureFrame.Release();
                        break;

                    case ImageReadMode.CPUAsync:
                    default:
                        req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        yield return waitUntilReqDone;
                        if (req.hasError)
                        {
                            Debug.LogWarning("Failed to read texture from the image source");
                            continue;
                        }
                        image = textureFrame.BuildCPUImage();
                        textureFrame.Release();
                        break;
                }

                switch (taskApi.runningMode)
                {
                    case Tasks.Vision.Core.RunningMode.IMAGE:
                        if (taskApi.TryDetect(image, imageProcessingOptions, ref result))
                        {
                            _annotationController.DrawNow(result);
                            _overlayController?.SetSourceTexture(imageSource.GetCurrentTexture());
                            _overlayController?.UpdateFromResult(result);
                        }
                        else _annotationController.DrawNow(default);
                        DisposeAllMasks(result);
                        break;

                    case Tasks.Vision.Core.RunningMode.VIDEO:
                        if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result))
                        {
                            _annotationController.DrawNow(result);
                            _overlayController?.SetSourceTexture(imageSource.GetCurrentTexture());
                            _overlayController?.UpdateFromResult(result);
                        }
                        else _annotationController.DrawNow(default);
                        DisposeAllMasks(result);
                        break;

                    case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
                        // El resultado llega en OnDetectionOutput
                        taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
                        break;
                }
            }
        }

        private void OnDetectionOutput(PoseLandmarkerResult result, Image image, long timestamp)
        {
            // No llamamos _annotationController.DrawLater() aquí porque el
            // annotation controller intenta instanciar GameObjects desde este
            // hilo secundario cuando OutputSegmentationMasks = true → UnityException.
            // GraduationOverlayController maneja todo visualmente de forma thread-safe.
            _overlayController?.SetSourceTexture(_imageSource?.GetCurrentTexture());
            _overlayController?.UpdateFromResult(result);
            DisposeAllMasks(result);
        }

        private void DisposeAllMasks(PoseLandmarkerResult result)
        {
            if (result.segmentationMasks == null) return;
            foreach (var mask in result.segmentationMasks)
                mask.Dispose();
        }
    }
}