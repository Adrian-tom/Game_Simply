// Filtr obrazu kamery 3D: poświata jasnych miejsc, kolor filmowy i miękkie przyciemnienie krawędzi. Interfejs go nie dotyczy.
Shader "GraSimply/Filtr"
{
    Properties
    {
        _MainTex ("Obraz", 2D) = "white" {}
        _Poswiata ("Poswiata", 2D) = "black" {}
        _Prog ("Prog poswiaty", Float) = 0.84
        _SilaPoswiaty ("Sila poswiaty", Float) = 0.38
        _Winieta ("Winieta", Float) = 0.42
        _Nasycenie ("Nasycenie", Float) = 1.12
        _Kontrast ("Kontrast", Float) = 1.1
        _Noc ("Noc", Float) = 0
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex, _Poswiata;
    float4 _MainTex_TexelSize;
    float _Prog, _SilaPoswiaty, _Winieta, _Nasycenie, _Kontrast, _Noc;

    float3 Rozmycie(float2 uv, float2 krok)
    {
        return tex2D(_MainTex, uv).rgb * 0.29
             + (tex2D(_MainTex, uv + krok * 1.38).rgb + tex2D(_MainTex, uv - krok * 1.38).rgb) * 0.24
             + (tex2D(_MainTex, uv + krok * 3.23).rgb + tex2D(_MainTex, uv - krok * 3.23).rgb) * 0.115;
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0: wybór jasnych miejsc
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            float4 frag(v2f_img i) : SV_Target
            {
                float3 k = tex2D(_MainTex, i.uv).rgb;
                float j = max(k.r, max(k.g, k.b));
                return float4(k * saturate((j - _Prog) / max(0.001, 1.0 - _Prog)), 1.0);
            }
            ENDCG
        }

        // 1: rozmycie poziome
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            float4 frag(v2f_img i) : SV_Target
            {
                return float4(Rozmycie(i.uv, float2(_MainTex_TexelSize.x, 0)), 1.0);
            }
            ENDCG
        }

        // 2: rozmycie pionowe
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            float4 frag(v2f_img i) : SV_Target
            {
                return float4(Rozmycie(i.uv, float2(0, _MainTex_TexelSize.y)), 1.0);
            }
            ENDCG
        }

        // 3: złożenie obrazu
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            float4 frag(v2f_img i) : SV_Target
            {
                float3 k = tex2D(_MainTex, i.uv).rgb + tex2D(_Poswiata, i.uv).rgb * _SilaPoswiaty;
                float jasnosc = dot(k, float3(0.299, 0.587, 0.114));
                k = lerp(jasnosc.xxx, k, _Nasycenie);
                k = saturate((k - 0.5) * _Kontrast + 0.5);
                // Cienie lekko chłodne, światła ciepłe; nocą całość idzie w błękit.
                k *= lerp(float3(0.95, 0.98, 1.05), float3(1.04, 1.01, 0.95), saturate(jasnosc * 1.4));
                k = lerp(k, k * float3(0.82, 0.92, 1.18), _Noc);
                float2 d = i.uv - 0.5;
                k *= 1.0 - _Winieta * smoothstep(0.32, 0.86, length(d * float2(1.05, 0.92)) * 1.25);
                return float4(k, 1.0);
            }
            ENDCG
        }
    }
    FallBack Off
}
