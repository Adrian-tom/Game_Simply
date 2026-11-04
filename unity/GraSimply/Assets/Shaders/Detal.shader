// Budynki i postacie obok realistycznej natury: stonowane barwy i drobna faktura ze zdjęcia nałożona w przestrzeni świata.
Shader "GraSimply/Detal"
{
    Properties
    {
        _MainTex ("Tekstura", 2D) = "white" {}
        _Detal ("Faktura", 2D) = "gray" {}
        _Skala ("Skala faktury", Float) = 1.3
        _SilaDetalu ("Sila faktury", Range(0, 1)) = 0.5
        _Nasycenie ("Nasycenie", Range(0, 1.2)) = 0.68
        _Odcien ("Odcien", Color) = (0.94, 0.9, 0.84, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex, _Detal;
        float _Skala, _SilaDetalu, _Nasycenie;
        float4 _Odcien;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 c = tex2D(_MainTex, IN.uv_MainTex).rgb;
            float jasnosc = dot(c, float3(0.299, 0.587, 0.114));
            c = lerp(jasnosc.xxx, c, _Nasycenie) * _Odcien.rgb;

            float3 w = pow(abs(IN.worldNormal), 4.0);
            w /= max(0.001, w.x + w.y + w.z);
            float3 p = IN.worldPos * _Skala;
            float3 d = tex2D(_Detal, p.zy).rgb * w.x + tex2D(_Detal, p.xz).rgb * w.y + tex2D(_Detal, p.xy).rgb * w.z;
            float faktura = dot(d, float3(0.299, 0.587, 0.114)) / 0.38;
            c *= lerp(1.0, faktura, _SilaDetalu);

            o.Albedo = saturate(c);
            o.Metallic = 0.0;
            o.Smoothness = 0.12;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
