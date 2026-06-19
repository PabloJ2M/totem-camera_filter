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

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uvWebcam = IN.uv;
                if (_FlipWebcam > 0.5) uvWebcam.y = 1.0 - uvWebcam.y;

                float2 uvMask = IN.uv;
                if (_FlipMask > 0.5) uvMask.y = 1.0 - uvMask.y;

                half3 person = SAMPLE_TEXTURE2D(_WebcamTex, sampler_WebcamTex, uvWebcam).rgb;
                half3 bg     = SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, IN.uv).rgb;

                // Blur 9 muestras para suavizar el recorte a bloques
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

                // La máscara de MediaPipe PoseLandmarker viene en .r (RFloat)
                // con 1.0 = persona, 0.0 = fondo — ya no necesitamos leer .a
                // ni pasar por el decodificador de BodyPix.
                half mask = smoothstep(0.5 - _MaskFeather, 0.5 + _MaskFeather, maskRaw);
                if (_InvertMask > 0.5) mask = 1.0 - mask;

                half3 finalColor = lerp(bg, person, mask);
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
