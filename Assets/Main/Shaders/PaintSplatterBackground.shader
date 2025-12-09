Shader "Custom/PaintSplatterBackground"
{
    Properties
    {
        _TimeScale ("Time Scale", Range(0, 5)) = 1.0
        _ScrollSpeed ("Scroll Speed", Vector) = (0.1, 0.1, 0, 0)
        _RotationSpeed ("Rotation Speed", Range(0, 2)) = 0.5
        _SplatterCount ("Splatter Count", Range(1, 20)) = 8
        _SplatterSizeMin ("Splatter Size Min", Range(0.1, 1)) = 0.2
        _SplatterSizeMax ("Splatter Size Max", Range(0.1, 1)) = 0.8
        _BlendAmount ("Blend Amount", Range(0, 1)) = 0.3
        _Color1 ("Color 1", Color) = (1, 0, 0, 1)
        _Color2 ("Color 2", Color) = (0, 0, 1, 1)
        _Color3 ("Color 3", Color) = (1, 1, 0, 1)
        _Color4 ("Color 4", Color) = (0, 1, 0, 1)
        _Color5 ("Color 5", Color) = (1, 0, 1, 1)
        _BackgroundColor ("Background Color", Color) = (1, 1, 1, 1)
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
            
            CBUFFER_START(UnityPerMaterial)
                float _TimeScale;
                float2 _ScrollSpeed;
                float _RotationSpeed;
                float _SplatterCount;
                float _SplatterSizeMin;
                float _SplatterSizeMax;
                float _BlendAmount;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float4 _Color4;
                float4 _Color5;
                float4 _BackgroundColor;
            CBUFFER_END
            
            // Simple noise function
            float noise(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            // Smooth noise
            float smoothNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Fractal noise
            float fractalNoise(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * smoothNoise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }
            
            // Create a splatter shape
            float createSplatter(float2 uv, float2 center, float size, float rotation, float time)
            {
                // Rotate UV around center
                float2 offset = uv - center;
                float cosR = cos(rotation);
                float sinR = sin(rotation);
                float2 rotated = float2(
                    offset.x * cosR - offset.y * sinR,
                    offset.x * sinR + offset.y * cosR
                );
                
                // Scale
                rotated /= size;
                
                // Distance from center
                float dist = length(rotated);
                
                // Base circular shape with noise
                float noiseValue = fractalNoise(rotated * 3.0 + time * 0.1);
                float shape = 1.0 - smoothstep(0.3, 0.8 + noiseValue * 0.3, dist);
                
                // Add noise variation
                shape *= (0.7 + noiseValue * 0.3);
                
                return shape;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y * _TimeScale;
                // Use wrapped time for animations to prevent overflow issues
                float wrappedTime = frac(time * 0.01) * 100.0; // Wrap every 100 seconds
                
                float2 uv = input.uv;
                
                // Scroll UV with wrapping (loop)
                float2 scrollOffset = _ScrollSpeed * time;
                uv = frac(uv + scrollOffset);
                
                // Initialize color
                float4 finalColor = _BackgroundColor;
                float alpha = 0.0;
                
                // Create multiple splatters
                for (int i = 0; i < (int)_SplatterCount; i++)
                {
                    // Generate pseudo-random values for each splatter
                    float seed = (float)i * 0.12345;
                    float2 center = float2(
                        frac(sin(seed * 12.9898) * 43758.5453),
                        frac(sin(seed * 78.233) * 43758.5453)
                    );
                    
                    // Animate center position slightly (using wrapped time to prevent overflow)
                    center += float2(
                        sin(wrappedTime * 0.3 + seed * 2.0) * 0.1,
                        cos(wrappedTime * 0.4 + seed * 3.0) * 0.1
                    );
                    // Ensure center stays within bounds
                    center = frac(center);
                    
                    // Random size
                    float sizeSeed = frac(sin(seed * 45.6789) * 43758.5453);
                    float size = lerp(_SplatterSizeMin, _SplatterSizeMax, sizeSeed);
                    
                    // Animate size (using wrapped time)
                    size *= (1.0 + sin(wrappedTime * 0.5 + seed * 4.0) * 0.2);
                    
                    // Random rotation (continuous rotation, no wrapping needed)
                    float rotation = (seed * 6.28318) + time * _RotationSpeed;
                    
                    // Create splatter shape
                    float splatterShape = createSplatter(uv, center, size, rotation, time);
                    
                    // Select color based on index
                    float4 splatterColor;
                    int colorIndex = i % 5;
                    if (colorIndex == 0) splatterColor = _Color1;
                    else if (colorIndex == 1) splatterColor = _Color2;
                    else if (colorIndex == 2) splatterColor = _Color3;
                    else if (colorIndex == 3) splatterColor = _Color4;
                    else splatterColor = _Color5;
                    
                    // Blend splatters
                    float splatterAlpha = splatterShape * (1.0 - _BlendAmount);
                    finalColor = lerp(finalColor, splatterColor, splatterAlpha);
                    alpha = max(alpha, splatterShape);
                }
                
                // Ensure minimum alpha for visibility
                alpha = max(alpha, 0.1);
                
                return half4(finalColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

