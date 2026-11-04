// Wypieczona natura: obrazek zawsze zwrócony do kamery, kołysany wiatrem, z cieniem i światłem doby.
// Pozycja wierzchołka to podstawa rośliny, a uv2 to przesunięcie narożnika w osiach kamery.
Shader "GraSimply/Natura"
{
    Properties
    {
        _MainTex ("Obraz", 2D) = "white" {}
        _Cutoff ("Prog alfa", Range(0, 1)) = 0.3
        _Sila ("Sila wiatru", Float) = 0.05
        _Odcien ("Odcien", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
        LOD 200
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:wierzcholek alphatest:_Cutoff addshadow
        #pragma target 3.0
        #pragma multi_compile_instancing

        sampler2D _MainTex;
        float _Sila;
        float4 _Odcien;
        float3 _KameraPrawo, _KameraGora, _KameraDo;

        struct Input
        {
            float2 uv_MainTex;
        };

        void wierzcholek(inout appdata_full v)
        {
            float3 podstawa = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
            float2 o = v.texcoord1.xy;
            float czas = _Time.y;
            float h = max(0.0, o.y);
            float podmuch = 0.55 + 0.45 * sin(czas * 0.63 + podstawa.x * 0.21 + podstawa.z * 0.17);
            float kolys = sin(czas * 1.6 + podstawa.x * 0.9 + podstawa.z * 0.7 + o.x * 1.3)
                        + 0.35 * sin(czas * 3.9 + podstawa.x * 2.3 + o.x * 4.0 + h * 3.0);
            float dx = kolys * podmuch * _Sila * h;
            // Lekko ku kamerze, żeby korzenie i dolne liście nie chowały się pod ziemią.
            float3 s = podstawa + _KameraPrawo * (o.x + dx) + _KameraGora * o.y + _KameraDo * 0.35;
            v.vertex = float4(mul(unity_WorldToObject, float4(s, 1.0)).xyz, 1.0);
            // Cieniowanie jest już w obrazku: normalna ku górze daje tylko jasność pory dnia.
            v.normal = normalize(mul((float3x3)unity_WorldToObject, normalize(float3(0, 0.8, 0) + _KameraDo * 0.6)));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float4 c = tex2D(_MainTex, IN.uv_MainTex);
            // Liście w cieniu własnej korony wychodzą szare: odrobina nasycenia oddaje im kolor.
            float l = dot(c.rgb, float3(0.299, 0.587, 0.114));
            o.Albedo = saturate(lerp(l.xxx, c.rgb, 1.35) * 1.3 * _Odcien.rgb);
            o.Alpha = c.a;
            o.Metallic = 0.0;
            o.Smoothness = 0.05;
        }
        ENDCG
    }
    FallBack "Transparent/Cutout/Diffuse"
}
