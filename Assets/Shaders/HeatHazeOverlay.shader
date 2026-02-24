Shader "Dungeons/FireForest/HeatHazeOverlay"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.62, 0.56, 0.52, 0.75)
        _Intensity ("Intensity", Range(0,1)) = 0
        _Flow ("Flow Speed", Range(0,4)) = 1.15
        _MoveDir ("Move Direction", Vector) = (1,0,0,0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _BaseColor;
            float _Intensity;
            float _Flow;
            float4 _MoveDir;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 dir = normalize(_MoveDir.xy + 1e-4);
                float t = _Time.y * _Flow;

                float2 uv0 = i.uv * float2(8.0, 5.5) + dir * t * 0.45;
                float2 uv1 = i.uv * float2(11.0, 8.0) - dir.yx * t * 0.32;
                float2 uv2 = i.uv * float2(18.0, 10.0) + dir * t * 0.18;

                float n0 = valueNoise(uv0);
                float n1 = valueNoise(uv1);
                float n2 = valueNoise(uv2);

                float waveX = sin((i.uv.y + t * 0.22) * 25.0 + dir.x * 3.14) * 0.5 + 0.5;
                float waveY = sin((i.uv.x - t * 0.17) * 31.0 - dir.y * 3.14) * 0.5 + 0.5;

                float haze = n0 * 0.45 + n1 * 0.28 + n2 * 0.17 + waveX * 0.06 + waveY * 0.04;
                haze = saturate((haze - 0.45) * 2.2);

                float alpha = haze * _Intensity * _BaseColor.a;
                float3 col = _BaseColor.rgb * (0.85 + haze * 0.25);

                return float4(col, alpha);
            }
            ENDHLSL
        }
    }
}
