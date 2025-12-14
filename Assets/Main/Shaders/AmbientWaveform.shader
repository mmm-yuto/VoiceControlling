Shader "Custom/AmbientWaveform"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [Header(Waveform Settings)]
        _WaveformIntensity ("Waveform Intensity", Range(0, 2)) = 0.05
        _WaveformSpeed ("Animation Speed", Range(0, 10)) = 1.0
        _WaveformColor ("Waveform Color", Color) = (0.5, 1.0, 1.0, 1.0)
        _WaveformColor2 ("Waveform Color 2", Color) = (1.0, 0.0, 0.0, 1.0)
        _ColorBlendFactor ("Color Blend Factor", Range(0, 1)) = 0.5
        _BorderWidth ("Border Width", Range(1, 100)) = 3.0
        [Header(Audio Reactive)]
        _AudioVolume ("Audio Volume (Normalized)", Range(0, 1)) = 0.0
        _AudioPitch ("Audio Pitch (Normalized)", Range(0, 1)) = 0.0
        _AudioReactiveIntensity ("Audio Reactive Intensity", Range(0, 10)) = 1.5
        _CycleSpeed ("Cycle Speed (Interpolated)", Range(0, 10)) = 0.5
        [Header(Bar Pattern)]
        _BarCount ("Bar Count", Range(8, 128)) = 32.0
        _BarWidth ("Bar Width", Range(0.5, 10)) = 2.0
        _MaxBarLength ("Max Bar Length", Range(0.1, 5)) = 0.3
        _CornerExclusionRatio ("Corner Exclusion Ratio", Range(0, 0.5)) = 0.1
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
                float4 _WaveformColor2;
                float _ColorBlendFactor;
                float _BorderWidth;
                float _AudioVolume;
                float _AudioPitch;
                float _AudioReactiveIntensity;
                float _BarCount;
                float _BarWidth;
                float _MaxBarLength;
                float _CornerExclusionRatio;
                float4 _CanvasSizeRatio; // (canvasWidthRatio, canvasHeightRatio, 0, 0)
                float _CycleSpeed; // 補間済みの周期速度（C#から渡される）
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
                // ピッチに応じて時間の速度を変化させる
                // ピッチが低い時：遅く（1.0倍）、ピッチが高い時：速く（10.0倍）
                float pitchSpeedMultiplier = lerp(1.0, 10.0, _AudioPitch);
                float time = _Time.y * _WaveformSpeed * pitchSpeedMultiplier;
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
                
                // 縁の幅を正規化（バーの表示範囲の判定に使用、ただし長さの制限には使用しない）
                float borderWidthNormalized = _BorderWidth / 100.0;
                borderWidthNormalized = saturate(borderWidthNormalized);
                
                // 各辺からの距離を計算
                float distFromTop = 1.0 - uv.y;
                float distFromBottom = uv.y;
                float distFromLeft = uv.x;
                float distFromRight = 1.0 - uv.x;
                
                // 外側の縁部分かどうかを判定（バーが長い場合でも表示されるように、判定範囲を広げる）
                // _MaxBarLengthに応じて判定範囲を拡張（画面からはみ出る長さも許可）
                float maxBarLengthNormalized = _MaxBarLength * 0.4; // バーの最大長さを正規化（baseBarLengthと同じ係数）
                // 判定範囲を広げる（最大で画面の200%以上も許可）
                float extendedBorderWidth = max(borderWidthNormalized, maxBarLengthNormalized * 1.5); // 1.5倍まで拡張
                bool isTopEdge = (uv.y > canvasMax.y) && (distFromTop < extendedBorderWidth);
                bool isBottomEdge = (uv.y < canvasMin.y) && (distFromBottom < extendedBorderWidth);
                bool isLeftEdge = (uv.x < canvasMin.x) && (distFromLeft < extendedBorderWidth);
                bool isRightEdge = (uv.x > canvasMax.x) && (distFromRight < extendedBorderWidth);
                
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
                
                // 角の部分を検出（各辺の端を除外）
                // 角の幅を設定（edgePositionの0-1の範囲で、端からこの範囲を除外）
                float cornerWidth = _CornerExclusionRatio; // Inspectorで設定可能な角の除外範囲
                bool isNearCorner = (edgePosition < cornerWidth || edgePosition > (1.0 - cornerWidth));
                
                // 角の近くではバーを表示しない（または短くする）
                if (isNearCorner)
                {
                    return half4(0, 0, 0, 0);
                }
                
                // 角を除外した範囲でedgePositionを再計算（0-1に正規化）
                float adjustedEdgePosition = (edgePosition - cornerWidth) / (1.0 - cornerWidth * 2.0);
                adjustedEdgePosition = saturate(adjustedEdgePosition); // 0-1にクランプ
                
                // バーのインデックスを計算（等間隔、角を除外した範囲で）
                float barIndex = floor(adjustedEdgePosition * _BarCount);
                float barPosition = (barIndex + 0.5) / _BarCount;
                
                // バーの幅内かどうかを判定
                float barWidthNormalized = _BarWidth / 100.0; // バーの幅を正規化
                float barStart = barPosition - barWidthNormalized * 0.5;
                float barEnd = barPosition + barWidthNormalized * 0.5;
                
                bool isInBar = (adjustedEdgePosition >= barStart && adjustedEdgePosition <= barEnd);
                
                if (!isInBar)
                {
                    return half4(0, 0, 0, 0);
                }
                
                // キャンバスの縁からの距離を計算（外側への距離、絶対値ベース）
                // UV座標系で計算するため、0-1の範囲で距離を測定
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
                
                // 距離が負の値（キャンバスの内側）の場合は0にクランプ
                distFromCanvasEdge = max(0.0, distFromCanvasEdge);
                
                // 各バーごとに異なるアニメーション速度（ランダムな位相）
                float barPhase = barIndex * 0.12345; // 各バーに異なる位相を設定
                
                // 周期速度はC#側で補間済み（_CycleSpeedを使用）
                // これにより、ピッチが変わっても周期が滑らかに変化する
                
                // 時間ベースのアニメーション（sin波で変化、周期は補間済みの_CycleSpeedを使用）
                float animationValue = sin(time * _CycleSpeed + barPhase) * 0.5 + 0.5; // 0-1の範囲
                
                // バーの長さを計算（時間ベースのアニメーション + 音声反応）
                // 縁の幅に依存しない絶対値ベースで計算
                // _MaxBarLengthを直接使用（0.1-5.0の範囲、画面サイズに対する倍率）
                // 0.1 = 画面の10%、5.0 = 画面の500%（はみ出してもOK）
                // UV座標系（0-1）で計算するため、_MaxBarLengthをそのまま使用可能な範囲に変換
                // _MaxBarLengthを直接使用して、画面からはみ出る長さも許可
                // 0.1 = 画面の10%、5.0 = 画面の500%（はみ出してもOK）
                // UV座標系（0-1）を基準に、_MaxBarLengthをそのまま使用
                // より長いバーを許可するため、係数を大きくする
                // 画面からはみ出る長さも許可（最大で画面の200%まで）
                float baseBarLength = _MaxBarLength * 0.4; // 0.1-5.0を0.04-2.0の範囲に変換（画面の4%-200%）
                
                // 音量が小さい時：小さなバー、音量が大きい時：大きなバー
                // アニメーションの範囲を常に一定に保ち、音量に応じて全体をシフト
                // これにより、音量が大きくても波が発生し続ける
                float animationRange = 0.3; // アニメーションの範囲（30%、常に一定）
                float minBarRatioBase = 0.2; // 最小値のベース（20%）
                float maxBarRatioBase = minBarRatioBase + animationRange; // 最大値のベース（50%）
                
                // 音量に応じてアニメーション範囲全体をシフト（範囲は一定に保つ）
                // _AudioVolumeはカリブレーション範囲から正規化された値（0-1）
                float volumeShift = _AudioVolume * 0.5; // 音量に応じて0-50%シフト
                float minBarRatio = minBarRatioBase + volumeShift; // 20%-70%の範囲
                float maxBarRatio = maxBarRatioBase + volumeShift; // 50%-100%の範囲
                
                // アニメーション値が0-1の範囲で変化するように、常に動く
                float animatedLength = baseBarLength * lerp(minBarRatio, maxBarRatio, animationValue);
                
                // 音声反応による追加長さ（各バーのアニメーション値に応じて変化させる）
                // これにより、音量が大きくても各バーが異なるサイズを保ち、波が常に見える
                float audioBoostBase = _AudioVolume * _AudioReactiveIntensity * baseBarLength * 0.3; // ベースの追加長さ
                // アニメーション値に応じて追加長さも変化させる（0.5-1.5倍の範囲）
                float audioBoostMultiplier = 0.5 + animationValue; // 0.5-1.5の範囲
                float audioBarLength = audioBoostBase * audioBoostMultiplier;
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
                
                // 2色のグラデーション対応：_ColorBlendFactorに応じて色を補間
                // _ColorBlendFactor = 1.0 の場合は _WaveformColor のみ
                // _ColorBlendFactor = 0.0 の場合は _WaveformColor2 のみ
                // 中間値の場合は2色を補間
                float3 color1 = _WaveformColor.rgb;
                float3 color2 = _WaveformColor2.rgb;
                float3 color = lerp(color2, color1, _ColorBlendFactor);
                
                float alpha = finalIntensity * barFade * lengthFade;
                
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

