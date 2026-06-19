Shader "Custom/BackgroundComposite"
{
    Properties
    {
        _WebcamTex      ("Webcam",     2D) = "white" {}
        _MaskTex        ("Mask",       2D) = "white" {}
        _BackgroundTex  ("Background", 2D) = "white" {}
        _MaskFeather    ("Mask Feather",        Range(0, 0.25)) = 0.05
        _MaskBlurRadius ("Mask Blur Radius (UV)", Range(0, 0.02)) = 0.006
        [Toggle] _FlipWebcam ("Flip Webcam Vertically", Float) = 0
        [Toggle] _FlipMask   ("Flip Mask Vertically",   Float) = 0
        [Toggle] _InvertMask ("Invert Mask",            Float) = 0
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

            float _MaskFeather;
            float _MaskBlurRadius;
            float _FlipWebcam;
            float _FlipMask;
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
                // Corrección opcional de orientación UV de la máscara
                float2 uvMask = i.uv;
                if (_FlipMask > 0) uvMask.y = 1.0 - uvMask.y;

                // Muestreo del Blur de 3x3
                float2 o = float2(_MaskBlurRadius, _MaskBlurRadius);
                half maskRaw = 0;
                maskRaw += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uvMask + float2(-o.x,-o.y)).r;
                maskRaw += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uvMask + float2( 0.0,-o.y)).r;
                maskRaw += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uvMask + float2( o.x,-o.y)).r;
                maskRaw += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uvMask + float2(-o.x, 0.0)).r;
                maskRaw += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uvMask + float2( 0.0, 0.0)).r;
                maskRaw += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uvMask + float2( o.x, 0.0)).r;
                maskRaw += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uvMask + float2(-o.x, o.y)).r;
                maskRaw += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uvMask + float2( 0.0, o.y)).r;
                maskRaw += SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, uvMask + float2( o.x, o.y)).r;
                maskRaw *= (1.0 / 9.0);

                // --- NUEVA LÓGICA DE CORTE CORREGIDA ---
                // Fijamos el centro en 0.4 (un umbral óptimo para MediaPipe) 
                // Creamos un rango simétrico usando _MaskFeather para el suavizado de bordes.
                half lowThreshold = clamp(0.4 - _MaskFeather, 0.01, 0.39);
                half highThreshold = clamp(0.4 + _MaskFeather, 0.41, 0.99);
                
                half mask = smoothstep(lowThreshold, highThreshold, maskRaw);

                // Opción de invertir máscara si está activada
                if (_InvertMask > 0) mask = 1.0 - mask;

                // Muestreo de la Webcam y Fondo
                float2 uvWebcam = i.uv;
                if (_FlipWebcam > 0) uvWebcam.y = 1.0 - uvWebcam.y;

                half4 webcamColor = SAMPLE_TEXTURE2D(_WebcamTex, sampler_WebcamTex, uvWebcam);
                half4 bgColor     = SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, i.uv);

                // Composición final: interpolación lineal limpia
                return lerp(bgColor, webcamColor, mask);
            }
            ENDHLSL
        }
    }
}
