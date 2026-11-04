// Drzewa, krzaki i trawa z wiatrem: im wyżej nad ziemią, tym mocniej się kołyszą, a podmuchy idą przez świat falą.
Shader "GraSimply/Roslina"
{
    Properties
    {
        _MainTex ("Tekstura", 2D) = "white" {}
        _Sila ("Sila wiatru", Float) = 0.055
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:wiatr addshadow
        #pragma target 3.0
        #pragma multi_compile_instancing

        sampler2D _MainTex;
        float _Sila;

        struct Input
        {
            float2 uv_MainTex;
        };

        void wiatr(inout appdata_full v)
        {
            float3 s = mul(unity_ObjectToWorld, v.vertex).xyz;
            float baza = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).y;
            float h = max(0.0, s.y - baza);
            float czas = _Time.y;
            float podmuch = 0.55 + 0.45 * sin(czas * 0.63 + s.x * 0.21 + s.z * 0.17);
            float kolys = sin(czas * 1.7 + s.x * 0.9 + s.z * 0.7) + 0.4 * sin(czas * 4.3 + s.x * 2.3 + s.y * 1.9);
            float3 przes = float3(0.8, 0.0, 0.45) * kolys * podmuch * _Sila * h * h;
            s += przes;
            v.vertex.xyz = mul(unity_WorldToObject, float4(s, 1.0)).xyz;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
            o.Metallic = 0.0;
            o.Smoothness = 0.08;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
