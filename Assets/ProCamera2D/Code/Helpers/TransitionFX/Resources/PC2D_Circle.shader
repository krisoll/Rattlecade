Shader "Hidden/ProCamera2D/TransitionsFX/Circle" 
{
    Properties 
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _Step ("Step", Range(0, 1)) = 0
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 1)
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

            float4 frag(v2f_img i) : COLOR 
            {
                float4 colour = _BackgroundColor;
                float aspectRatio = _ScreenParams.y / _ScreenParams.x;
                if (sqrt(pow(i.uv.x - 0.5, 2) + pow((i.uv.y - 0.5) * aspectRatio, 2) < 0.5 - (_Step / 2)))
                    colour = tex2D(_MainTex, i.uv);
                
                return colour;
            }

            ENDCG
        }
    }
}