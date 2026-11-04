// Tylko do wypieku natury w edytorze: pełna jakość z przezroczystością liści, alfa zostaje w obrazie.
Shader "GraSimply/Wypiek"
{
    Properties
    {
        _MainTex ("Kolor", 2D) = "white" {}
        _Alfa ("Alfa", 2D) = "white" {}
        _Normalne ("Normalne", 2D) = "bump" {}
    }
    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows keepalpha addshadow
        #pragma target 3.0

        sampler2D _MainTex, _Alfa, _Normalne;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            clip(tex2D(_Alfa, IN.uv_MainTex).r - 0.5);
            o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
            o.Normal = UnpackNormal(tex2D(_Normalne, IN.uv_MainTex));
            o.Metallic = 0.0;
            o.Smoothness = 0.1;
            o.Alpha = 1.0;
        }
        ENDCG
    }
}
