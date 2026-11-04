// Ziemia całego świata: cztery zdjęciowe tekstury CC0 mieszane mapą składu, a do tego żywa woda.
// _Sklad.rgba = udział trawy, ziemi, skały i piasku na polu; _Barwa.rgb = odcień biomu, _Barwa.a = woda.
Shader "GraSimply/Grunt"
{
    Properties
    {
        _Sklad ("Sklad", 2D) = "red" {}
        _Barwa ("Barwa i woda", 2D) = "white" {}
        _Trawa ("Trawa", 2D) = "white" {}
        _TrawaN ("Trawa normalne", 2D) = "bump" {}
        _Ziemia ("Ziemia", 2D) = "white" {}
        _ZiemiaN ("Ziemia normalne", 2D) = "bump" {}
        _Skala ("Skala", 2D) = "white" {}
        _SkalaN ("Skala normalne", 2D) = "bump" {}
        _Piasek ("Piasek", 2D) = "white" {}
        _PiasekN ("Piasek normalne", 2D) = "bump" {}
        _Skalowanie ("Powtorzen na jednostke", Float) = 0.28
        _Snieg ("Snieg", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.5

        sampler2D _Sklad, _Barwa;
        sampler2D _Trawa, _TrawaN, _Ziemia, _ZiemiaN, _Skala, _SkalaN, _Piasek, _PiasekN;
        float _Skalowanie, _Snieg;

        struct Input
        {
            float2 uv_Sklad;
            float3 worldPos;
        };

        // Tani szum bez tekstury: rwie granice między warstwami i odcięcia brzegu wody.
        float Szum(float2 p)
        {
            float2 i = floor(p), f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            float a = frac(sin(dot(i, float2(127.1, 311.7))) * 43758.5453);
            float b = frac(sin(dot(i + float2(1, 0), float2(127.1, 311.7))) * 43758.5453);
            float c = frac(sin(dot(i + float2(0, 1), float2(127.1, 311.7))) * 43758.5453);
            float d = frac(sin(dot(i + float2(1, 1), float2(127.1, 311.7))) * 43758.5453);
            return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
        }

        float Fale(float2 p)
        {
            float2 r = p * 1.7;
            return Szum(r + _Time.y * float2(0.31, 0.17)) * 0.6 + Szum(r * 2.3 - _Time.y * float2(0.22, 0.37)) * 0.4;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.worldPos.xz * _Skalowanie;
            float szum = Szum(IN.worldPos.xz * 1.3) * 0.55 + Szum(IN.worldPos.xz * 4.1) * 0.45;
            float plama = Szum(IN.worldPos.xz * 0.23);

            // Drugi obrót tej samej tekstury w innej skali: pole nie wygląda jak powtarzany kafel.
            float2 uv2 = float2(uv.y, -uv.x) * 0.37 + 0.31;

            float4 w = tex2D(_Sklad, IN.uv_Sklad);
            w = pow(saturate(w * (0.55 + szum * 0.9)), 2.2) + 1e-4;
            w /= dot(w, 1.0);

            float3 tr = lerp(tex2D(_Trawa, uv).rgb, tex2D(_Trawa, uv2).rgb, 0.45);
            float3 zi = lerp(tex2D(_Ziemia, uv).rgb, tex2D(_Ziemia, uv2).rgb, 0.45);
            float3 kolor = tr * w.r + zi * w.g + tex2D(_Skala, uv).rgb * w.b + tex2D(_Piasek, uv).rgb * w.a;
            float3 normal = UnpackNormal(tex2D(_TrawaN, uv)) * w.r + UnpackNormal(tex2D(_ZiemiaN, uv)) * w.g
                          + UnpackNormal(tex2D(_SkalaN, uv)) * w.b + UnpackNormal(tex2D(_PiasekN, uv)) * w.a;

            float4 barwa = tex2D(_Barwa, IN.uv_Sklad);
            kolor *= barwa.rgb * 1.22 * lerp(0.84, 1.14, plama);
            kolor = lerp(kolor, float3(0.9, 0.93, 0.97), _Snieg * saturate(0.6 + szum * 0.5));
            float gladkosc = 0.06 * (w.g + w.a);

            // Woda: fale, ciemna głębia i jasny pas piany przy brzegu.
            float woda = barwa.a + (szum - 0.5) * 0.22;
            float maska = smoothstep(0.46, 0.54, woda);
            if (maska > 0.001)
            {
                float f = Fale(IN.worldPos.xz);
                float e = 0.06;
                float dx = Fale(IN.worldPos.xz + float2(e, 0)) - f;
                float dz = Fale(IN.worldPos.xz + float2(0, e)) - f;
                float3 nw = normalize(float3(-dx * 3.2, -dz * 3.2, 1.0));
                float glebia = smoothstep(0.54, 0.9, woda);
                float3 kw = lerp(float3(0.13, 0.42, 0.47), float3(0.03, 0.17, 0.3), glebia) + f * 0.06;
                float piana = (1.0 - smoothstep(0.5, 0.6, woda)) * smoothstep(0.42, 0.72, f);
                kw = lerp(kw, float3(0.86, 0.93, 0.95), piana * 0.8);
                kolor = lerp(kolor, kw, maska);
                normal = lerp(normal, nw, maska);
                gladkosc = lerp(gladkosc, 0.82, maska);
            }

            o.Albedo = saturate(kolor);
            o.Normal = normalize(normal);
            o.Metallic = 0.0;
            o.Smoothness = gladkosc;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
