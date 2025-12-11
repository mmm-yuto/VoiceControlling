Shader "Custom/AmbientWaveform"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [Header(Waveform Settings)]
        _WaveformIntensity ("Waveform Intensity", Range(0, 2)) = 0.05
        _WaveformSpeed ("Waveform Speed", Range(0, 10)) = 1.0
        _WaveformColor ("Waveform Color", Color) = (0.5, 1.0, 1.0, 1.0)
        _BorderWidth ("Border Width", Range(1, 100)) = 3.0
        [Header(Audio Reactive)]
        _AudioVolume ("Audio Volume", Range(0, 1)) = 0.0
        _AudioReactiveIntensity ("Audio Reactive Intensity", Range(0, 10)) = 1.5
        [Header(Waveform Pattern)]
        _WaveFrequency ("Wave Frequency", Range(1, 200)) = 10.0
        _WaveAmplitude ("Wave Amplitude", Range(0.1, 5.0)) = 0.5
        [Header(Internal)]
        _CanvasSizeRatio ("Canvas Size Ratio", Vector) = (1, 1, 0, 0)
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _WaveformIntensity;
                float _WaveformSpeed;
                float4 _WaveformColor;
                float _BorderWidth;
                float _AudioVolume;
                float _AudioReactiveIntensity;
                float _WaveFrequency;
                float _WaveAmplitude;
                float4 _CanvasSizeRatio; // (canvasWidthRatio, canvasHeightRatio, 0, 0)
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y * _WaveformSpeed;
                float2 uv = input.uv;
                
                // キャンバスのサイズ比率を取得
                float canvasWidthRatio = _CanvasSizeRatio.x;
                float canvasHeightRatio = _CanvasSizeRatio.y;
                
                // キャンバスの中心とサイズを計算
                float2 canvasCenter = float2(0.5, 0.5);
                float2 canvasHalfSize = float2(canvasWidthRatio * 0.5, canvasHeightRatio * 0.5);
                float2 canvasMin = canvasCenter - canvasHalfSize;
                float2 canvasMax = canvasCenter + canvasHalfSize;
                
                // キャンバスの内側かどうかを判定
                bool isInsideCanvas = (uv.x >= canvasMin.x && uv.x <= canvasMax.x && 
                                      uv.y >= canvasMin.y && uv.y <= canvasMax.y);
                
                // 内側の場合は透明（外側の縁のみ表示）
                if (isInsideCanvas)
                {
                    return half4(0, 0, 0, 0);
                }
                
                // 縁の幅を正規化（0-1の範囲で）
                float borderWidthNormalized = _BorderWidth / 100.0;
                borderWidthNormalized = saturate(borderWidthNormalized);
                
                // 各辺からの距離を計算（Imageの外側の縁を判定）
                float distFromTop = 1.0 - uv.y;
                float distFromBottom = uv.y;
                float distFromLeft = uv.x;
                float distFromRight = 1.0 - uv.x;
                
                // 外側の縁部分かどうかを判定（キャンバスの外側で、かつ縁の幅内）
                bool isTopEdge = (uv.y > canvasMax.y) && (distFromTop < borderWidthNormalized);
                bool isBottomEdge = (uv.y < canvasMin.y) && (distFromBottom < borderWidthNormalized);
                bool isLeftEdge = (uv.x < canvasMin.x) && (distFromLeft < borderWidthNormalized);
                bool isRightEdge = (uv.x > canvasMax.x) && (distFromRight < borderWidthNormalized);
                
                bool isBorder = isTopEdge || isBottomEdge || isLeftEdge || isRightEdge;
                
                // 縁部分でない場合は透明
                if (!isBorder)
                {
                    return half4(0, 0, 0, 0);
                }
                
                // 縁に沿った距離を計算（波形の連続性のため）
                // 外側の縁を上辺→右辺→下辺→左辺の順で一周する
                float borderDistance = 0.0;
                float perimeter = 0.0; // 周長を計算
                
                // 各辺の長さを計算
                float topEdgeLength = 1.0; // 上辺の長さ（Imageの幅全体）
                float rightEdgeLength = canvasMin.y + (1.0 - canvasMax.y); // 右辺の長さ（上側の縁 + 下側の縁）
                float bottomEdgeLength = 1.0; // 下辺の長さ（Imageの幅全体）
                float leftEdgeLength = canvasMin.y + (1.0 - canvasMax.y); // 左辺の長さ（上側の縁 + 下側の縁）
                perimeter = topEdgeLength + rightEdgeLength + bottomEdgeLength + leftEdgeLength;
                
                if (isTopEdge && !isRightEdge)
                {
                    // 上辺：左から右へ（キャンバスの上側）
                    borderDistance = uv.x;
                }
                else if (isRightEdge && !isBottomEdge)
                {
                    // 右辺：上から下へ（キャンバスの右側）
                    borderDistance = topEdgeLength + (uv.y - canvasMax.y);
                }
                else if (isBottomEdge && !isLeftEdge)
                {
                    // 下辺：右から左へ（キャンバスの下側、連続性のため）
                    borderDistance = topEdgeLength + rightEdgeLength + (1.0 - uv.x);
                }
                else if (isLeftEdge)
                {
                    // 左辺：下から上へ（キャンバスの左側、連続性のため）
                    borderDistance = topEdgeLength + rightEdgeLength + bottomEdgeLength + (canvasMin.y - uv.y);
                }
                else
                {
                    // 角の場合は、最も近い辺を使用
                    if (isTopEdge) borderDistance = uv.x;
                    else if (isRightEdge) borderDistance = topEdgeLength + (uv.y - canvasMax.y);
                    else if (isBottomEdge) borderDistance = topEdgeLength + rightEdgeLength + (1.0 - uv.x);
                    else borderDistance = topEdgeLength + rightEdgeLength + bottomEdgeLength + (canvasMin.y - uv.y);
                }
                
                // 縁に沿った距離を0-1に正規化（周長で割る）
                float normalizedDistance = borderDistance / perimeter;
                
                // 波形パターンを生成（sin波）
                float wave = sin(normalizedDistance * _WaveFrequency * 6.28318 + time * 2.0) * _WaveAmplitude;
                
                // 縁からの距離に応じてフェードアウト（より滑らかに）
                float edgeFade = 1.0;
                if (isTopEdge)
                {
                    edgeFade = smoothstep(borderWidthNormalized, 0.0, distFromTop);
                }
                else if (isBottomEdge)
                {
                    edgeFade = smoothstep(borderWidthNormalized, 0.0, distFromBottom);
                }
                else if (isRightEdge)
                {
                    edgeFade = smoothstep(borderWidthNormalized, 0.0, distFromRight);
                }
                else if (isLeftEdge)
                {
                    edgeFade = smoothstep(borderWidthNormalized, 0.0, distFromLeft);
                }
                
                // 波形の強度を計算（基本強度 + 音声反応）
                float baseIntensity = _WaveformIntensity;
                float audioBoost = _AudioVolume * _AudioReactiveIntensity;
                float finalIntensity = baseIntensity * (1.0 + audioBoost);
                
                // 波形の値を0-1に正規化
                float waveValue = (wave + _WaveAmplitude) / (_WaveAmplitude * 2.0);
                
                // 最終的な色とアルファ
                float3 color = _WaveformColor.rgb;
                float alpha = waveValue * finalIntensity * edgeFade;
                
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

