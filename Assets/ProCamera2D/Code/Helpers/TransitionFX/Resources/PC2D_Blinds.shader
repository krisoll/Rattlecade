Shader "Hidden/ProCamera2D/TransitionsFX/Blinds" 
{
    Properties 
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _Step ("Step", Range(0, 1)) = 0
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 1)
        _Direction ("Direction", Int) = 0
        _Blinds ("Blinds", Int) = 2
    }

    SubShader 
    {
        ZTest Always Cull Off ZWrite Off Fog{ Mode Off }

        Pass 
        {
            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc" 

            uniform sampler2D _MainTex;
            uniform float _Step;
            uniform float4 _BackgroundColor;
            uniform int _Direction;
            uniform int _Blinds;

            float4 frag(v2f_img i) : Color 
            {
                float4 colour = _BackgroundColor;

                if (_Direction == 0 && frac(i.uv.x * _Blinds) < 1 - _Step)
                    colour = tex2D(_MainTex, i.uv);
                else if (_Direction == 1 && frac(i.uv.x * _Blinds) > _Step)
                    colour = tex2D(_MainTex, i.uv);
                else if (_Direction == 2 && frac(i.uv.y * _Blinds) > _Step)
                    colour = tex2D(_MainTex, i.uv);
                else if (_Direction == 3 && frac(i.uv.y * _Blinds) < 1 - _Step)
                    colour = tex2D(_MainTex, i.uv);

                return colour;
            }

            ENDCG
        }
    }
}