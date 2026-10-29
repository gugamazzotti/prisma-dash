// Lit color with a single directional light and optional glow.
// No shader_feature keywords: a handful of fog variants, not the Standard shader's hundreds of thousands.
Shader "PrismaDash/Color"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Emission ("Emission", Range(0, 2)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            fixed4 _Color;
            float _Emission;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNormal);
                float ndotl = saturate(dot(n, _WorldSpaceLightPos0.xyz));
                float3 col = _Color.rgb * (unity_AmbientSky.rgb + _LightColor0.rgb * ndotl);
                col += _Color.rgb * _Emission;
                fixed4 c = fixed4(saturate(col), 1);
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
    FallBack Off
}
