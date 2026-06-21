Shader "Custom/BackgroundComposite"
{
    Properties
    {
        _WebcamTex      ("Webcam",     2D) = "white" {}
        _MaskTex        ("Mask",       2D) = "white" {}
        _BackgroundTex  ("Background", 2D) = "white" {}
        _MaskCutoff     ("Mask Cutoff (Borde)",   Range(0.1, 0.9)) = 0.4
        _MaskSmooth     ("Mask Smoothness",       Range(0.0, 0.2)) = 0.05
        [Toggle] _FlipWebcamY ("Flip Webcam Vertical",   Float) = 0
        [Toggle] _FlipWebcamX ("Flip Webcam Horizontal", Float) = 0
        [Toggle] _FlipMaskY   ("Flip Mask Vertical",     Float) = 0
        [Toggle] _FlipMaskX   ("Flip Mask Horizontal",   Float) = 0
        [Toggle] _InvertMask  ("Invert Mask",            Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "Composite"
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_WebcamTex);     SAMPLER(sampler_WebcamTex);
            TEXTURE2D(_MaskTex);       SAMPLER(sampler_MaskTex);
            TEXTURE2D(_BackgroundTex); SAMPLER(sampler_BackgroundTex);

            float _MaskCutoff;
            float _MaskSmooth;
            float _FlipWebcamY;
            float _FlipWebcamX;
            float _FlipMaskY;
            float _FlipMaskX;
            float _InvertMask;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // UV de la máscara con flip independiente en X e Y
                float2 uvMask = i.uv;
                if (_FlipMaskY > 0) uvMask.y = 1.0 - uvMask.y;
                if (_FlipMaskX > 0) uvMask.x = 1.0 - uvMask.x;

                // [OPTIMIZACIÓN GAMA BAJA] 1 sola lectura de textura en vez de 9.
                half maskRaw = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uvMask).r;

                // Suavizado inteligente basado en hardware + control de usuario
                // fwidth() ayuda a que el borde no se pixelee si te acercas o alejas
                float edgeDelta = _MaskSmooth + fwidth(maskRaw); 
                half lowThreshold  = clamp(_MaskCutoff - edgeDelta, 0.0, 0.95);
                half highThreshold = clamp(_MaskCutoff + edgeDelta, 0.05, 1.0);
                
                half mask = smoothstep(lowThreshold, highThreshold, maskRaw);

                if (_InvertMask > 0) mask = 1.0 - mask;

                // UV de la webcam con flip independiente en X e Y
                float2 uvWebcam = i.uv;
                if (_FlipWebcamY > 0) uvWebcam.y = 1.0 - uvWebcam.y;
                if (_FlipWebcamX > 0) uvWebcam.x = 1.0 - uvWebcam.x;

                half4 webcamColor = SAMPLE_TEXTURE2D(_WebcamTex, sampler_WebcamTex, uvWebcam);
                half4 bgColor     = SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, i.uv);

                return lerp(bgColor, webcamColor, mask);
            }
            ENDHLSL
        }
    }
}