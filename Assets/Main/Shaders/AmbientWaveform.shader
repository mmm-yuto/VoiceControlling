Shader "Custom/AmbientWaveform"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [Header(Waveform Settings)]
        _WaveformIntensity ("Waveform Intensity", Range(0, 2)) = 0.05
        _WaveformSpeed ("Animation Speed", Range(0, 10)) = 1.0
        _WaveformColor ("Waveform Color", Color) = (0.5, 1.0, 1.0, 1.0)
        _BorderWidth ("Border Width", Range(1, 100)) = 3.0
        [Header(Audio Reactive)]
        _AudioVolume ("Audio Volume", Range(0, 1)) = 0.0
        _AudioReactiveIntensity ("Audio Reactive Intensity", Range(0, 10)) = 1.5
        [Header(Bar Pattern)]
        _BarCount ("Bar Count", Range(8, 128)) = 32.0
        _BarWidth ("Bar Width", Range(0.5, 10)) = 2.0
        _MaxBarLength ("Max Bar Length", Range(0.1, 1)) = 0.3
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
                float _BarCount;
                float _BarWidth;
                float _MaxBarLength;
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
                
                // 縁の幅を正規化
                float borderWidthNormalized = _BorderWidth / 100.0;
                borderWidthNormalized = saturate(borderWidthNormalized);
                
                // 各辺からの距離を計算
                float distFromTop = 1.0 - uv.y;
                float distFromBottom = uv.y;
                float distFromLeft = uv.x;
                float distFromRight = 1.0 - uv.x;
                
                // 外側の縁部分かどうかを判定
                bool isTopEdge = (uv.y > canvasMax.y) && (distFromTop < borderWidthNormalized);
                bool isBottomEdge = (uv.y < canvasMin.y) && (distFromBottom < borderWidthNormalized);
                bool isLeftEdge = (uv.x < canvasMin.x) && (distFromLeft < borderWidthNormalized);
                bool isRightEdge = (uv.x > canvasMax.x) && (distFromRight < borderWidthNormalized);
                
                bool isBorder = isTopEdge || isBottomEdge || isLeftEdge || isRightEdge;
                
                if (!isBorder)
                {
                    return half4(0, 0, 0, 0);
                }
                
                // 縁に沿った位置を計算（0-1に正規化）
                float edgePosition = 0.0;
                float edgeLength = 0.0;
                
                if (isTopEdge)
                {
                    // 上辺：左から右へ
                    edgePosition = (uv.x - canvasMin.x) / (canvasMax.x - canvasMin.x);
                    edgeLength = canvasMax.x - canvasMin.x;
                }
                else if (isBottomEdge)
                {
                    // 下辺：左から右へ
                    edgePosition = (uv.x - canvasMin.x) / (canvasMax.x - canvasMin.x);
                    edgeLength = canvasMax.x - canvasMin.x;
                }
                else if (isRightEdge)
                {
                    // 右辺：上から下へ
                    edgePosition = (uv.y - canvasMin.y) / (canvasMax.y - canvasMin.y);
                    edgeLength = canvasMax.y - canvasMin.y;
                }
                else if (isLeftEdge)
                {
                    // 左辺：上から下へ
                    edgePosition = (uv.y - canvasMin.y) / (canvasMax.y - canvasMin.y);
                    edgeLength = canvasMax.y - canvasMin.y;
                }
                
                // バーのインデックスを計算（等間隔）
                float barIndex = floor(edgePosition * _BarCount);
                float barPosition = (barIndex + 0.5) / _BarCount;
                
                // バーの幅内かどうかを判定
                float barWidthNormalized = _BarWidth / 100.0; // バーの幅を正規化
                float barStart = barPosition - barWidthNormalized * 0.5;
                float barEnd = barPosition + barWidthNormalized * 0.5;
                
                bool isInBar = (edgePosition >= barStart && edgePosition <= barEnd);
                
                if (!isInBar)
                {
                    return half4(0, 0, 0, 0);
                }
                
                // キャンバスの縁からの距離を計算（外側への距離）
                float distFromCanvasEdge = 0.0;
                
                if (isTopEdge)
                {
                    distFromCanvasEdge = uv.y - canvasMax.y;
                }
                else if (isBottomEdge)
                {
                    distFromCanvasEdge = canvasMin.y - uv.y;
                }
                else if (isRightEdge)
                {
                    distFromCanvasEdge = uv.x - canvasMax.x;
                }
                else if (isLeftEdge)
                {
                    distFromCanvasEdge = canvasMin.x - uv.x;
                }
                
                // 各バーごとに異なるアニメーション速度（ランダムな位相）
                float barPhase = barIndex * 0.12345; // 各バーに異なる位相を設定
                
                // 音声のボリュームに応じてアニメーション周期を変化
                // 音量が小さい時：周期が低い（ゆっくり）、音量が大きい時：周期が高い（速く）
                float minCycleSpeed = 0.5; // 最小周期速度（音量0の時）
                float maxCycleSpeed = 3.0; // 最大周期速度（音量1の時）
                float cycleSpeed = lerp(minCycleSpeed, maxCycleSpeed, _AudioVolume);
                
                // 時間ベースのアニメーション（sin波で変化、周期は音量に応じて変化）
                float animationValue = sin(time * cycleSpeed + barPhase) * 0.5 + 0.5; // 0-1の範囲
                
                // バーの長さを計算（時間ベースのアニメーション + 音声反応）
                float baseBarLength = _MaxBarLength * borderWidthNormalized;
                
                // 音量が小さい時：小さなバー、音量が大きい時：大きなバー
                // アニメーションの最小値と最大値を音量に応じて調整
                float minBarRatio = 0.2 + _AudioVolume * 0.3; // 20%-50%の範囲（音量に応じて）
                float maxBarRatio = 0.5 + _AudioVolume * 0.5; // 50%-100%の範囲（音量に応じて）
                float animatedLength = baseBarLength * lerp(minBarRatio, maxBarRatio, animationValue);
                
                // 音声反応による追加長さ（波の効果を強化）
                float audioBarLength = _AudioVolume * _AudioReactiveIntensity * baseBarLength;
                float totalBarLength = animatedLength + audioBarLength;
                
                // バーの長さ内かどうかを判定
                if (distFromCanvasEdge < 0.0 || distFromCanvasEdge > totalBarLength)
                {
                    return half4(0, 0, 0, 0);
                }
                
                // バーの幅に応じたフェード（端を滑らかに）
                float barFade = 1.0 - smoothstep(0.0, barWidthNormalized * 0.3, abs(edgePosition - barPosition));
                
                // バーの長さに応じたフェード（先端を滑らかに）
                float lengthFade = 1.0 - smoothstep(totalBarLength * 0.7, totalBarLength, distFromCanvasEdge);
                
                // 波形の強度を計算（基本強度 + 音声反応）
                float baseIntensity = _WaveformIntensity;
                float audioBoost = _AudioVolume * _AudioReactiveIntensity;
                float finalIntensity = baseIntensity * (1.0 + audioBoost);
                
                // 最終的な色とアルファ
                float3 color = _WaveformColor.rgb;
                float alpha = finalIntensity * barFade * lengthFade;
                
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

