// Cracked glass that refracts and blurs the scene behind it (URP opaque texture). The mesh carries an edge weight
// in uv2.x (0 at the centre, 1 at the jagged edge, see GlassShardRules) that scales refraction, blur and cracks.
Shader "BossFight/GlassShard"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.86, 0.93, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 1
        _Refraction ("Edge Refraction (uv)", Range(0, 0.08)) = 0.03
        _BlurRadius ("Edge Blur Radius (uv)", Range(0, 0.03)) = 0.012
        _SheenStrength ("Sheen Strength", Range(0, 2)) = 0.6
        _SheenSharpness ("Sheen Sharpness", Range(1, 256)) = 48
        _CrackColor ("Crack Color", Color) = (0.95, 0.97, 1, 0.85)
        _CrackScale ("Crack Cell Scale", Float) = 6
        _CrackWidth ("Crack Line Width", Range(0.001, 0.2)) = 0.035
        _CrackCoverage ("Crack Coverage (0 edges only, 1 everywhere)", Range(0, 1)) = 0
        _CrackTex ("Crack Texture (optional)", 2D) = "black" {}
        _CrackTexStrength ("Crack Texture Strength", Range(0, 1)) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "GlassShard"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest [_ZTest]
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_CrackTex);
            SAMPLER(sampler_CrackTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Opacity;
                float _Refraction;
                float _BlurRadius;
                half _SheenStrength;
                half _SheenSharpness;
                half4 _CrackColor;
                float _CrackScale;
                float _CrackWidth;
                half _CrackCoverage;
                float4 _CrackTex_ST;
                half _CrackTexStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 edge : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float edge : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.edge = input.edge.x;
                return output;
            }

            float2 Hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            // Distance to the nearest Voronoi cell border: shards of broken glass.
            float CellBorder(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                float2 nearest = 0;
                float best = 8;
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float2 offset = float2(x, y) + Hash22(cell + float2(x, y)) - local;
                    float d = dot(offset, offset);
                    if (d < best) { best = d; nearest = offset; }
                }
                float border = 8;
                for (int y2 = -1; y2 <= 1; y2++)
                for (int x2 = -1; x2 <= 1; x2++)
                {
                    float2 offset = float2(x2, y2) + Hash22(cell + float2(x2, y2)) - local;
                    float2 between = offset - nearest;
                    if (dot(between, between) > 1e-5)
                        border = min(border, dot(0.5 * (nearest + offset), normalize(between)));
                }
                return border;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float w = saturate(input.edge);
                float2 fromCentre = input.uv - 0.5;
                float2 bend = normalize(fromCentre + 1e-5) * w + (Hash22(floor(input.uv * _CrackScale * 2)) - 0.5) * w * 0.5;

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float2 refracted = screenUV - bend * _Refraction;

                // 9-tap disc blur whose radius grows with the edge weight.
                float radius = _BlurRadius * w;
                float3 scene = SampleSceneColor(refracted);
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 0.785398;
                    scene += SampleSceneColor(refracted + float2(cos(angle), sin(angle)) * radius);
                }
                scene /= 9;

                // Glossy sheen from a tilted fake normal plus a diagonal streak.
                float3 normal = normalize(float3(bend * 0.6, 1));
                float3 halfway = normalize(float3(-0.35, 0.55, 1) + float3(0, 0, 1));
                half spec = pow(saturate(dot(normal, halfway)), _SheenSharpness);
                half streak = pow(saturate(1 - abs(dot(fromCentre, float2(0.7071, 0.7071)) * 3 - 0.35)), 12);
                half sheen = (spec + streak * 0.5) * _SheenStrength;

                // Cracks: procedural cell borders that fade in toward the edge, plus the optional texture.
                half crackMask = lerp(smoothstep(0.3, 0.95, w), 1, _CrackCoverage);
                half cellLine = 1 - smoothstep(0, _CrackWidth, CellBorder(input.uv * _CrackScale));
                half texCrack = SAMPLE_TEXTURE2D(_CrackTex, sampler_CrackTex, TRANSFORM_TEX(input.uv, _CrackTex)).r * _CrackTexStrength;
                half crack = saturate(cellLine * crackMask + texCrack) * _CrackColor.a;

                half3 color = scene * _Tint.rgb + sheen + w * w * 0.08;
                color = lerp(color, _CrackColor.rgb, crack);
                return half4(color, _Opacity);
            }
            ENDHLSL
        }
    }
}
