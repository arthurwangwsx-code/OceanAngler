Shader "OceanAngler/BayWater"
{
    Properties
    {
        _ShallowColor("Shallow water",Color)=(0.12,0.54,0.57,1)
        _DeepColor("Deep water",Color)=(0.025,0.20,0.27,1)
        _FoamColor("Shore foam",Color)=(0.72,0.87,0.82,1)
        _OceanTime("Water time",Float)=0
        _SeaLevel("Sea level",Float)=4.5
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
        Pass
        {
            Name "Water"
            Tags {"LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _ShallowColor,_DeepColor,_FoamColor;
            float _OceanTime,_SeaLevel;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;half fog:TEXCOORD2;};
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world=TransformObjectToWorld(input.positionOS.xyz);
                float a=world.x*.82+world.z*.43+_OceanTime*.65;
                float b=world.z*1.4-world.x*.31-_OceanTime*.83;
                world.y+=sin(a)*.035+sin(b)*.018;
                float dx=cos(a)*.035*.82-cos(b)*.018*.31;
                float dz=cos(a)*.035*.43+cos(b)*.018*1.4;
                output.positionWS=world;output.positionCS=TransformWorldToHClip(world);output.normalWS=normalize(float3(-dx,1,-dz));output.fog=ComputeFogFactor(output.positionCS.z);return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 uv=GetNormalizedScreenSpaceUV(input.positionCS);
                float depth=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);
                float eyeDepth=-TransformWorldToView(input.positionWS).z;
                float thickness=max(0,depth-eyeDepth);
                float3 view=normalize(GetWorldSpaceViewDir(input.positionWS));
                half3 normal=normalize(input.normalWS);bool above=_WorldSpaceCameraPos.y>_SeaLevel;
                if(!above)normal=-normal;
                half fresnel=pow(1-saturate(dot(view,normal)),4);
                half absorb=1-exp(-thickness*.16);
                half3 tint=lerp(_ShallowColor.rgb,_DeepColor.rgb,absorb);
                // Fade distortion near intersections to avoid pulling foreground objects across the shoreline.
                float2 offset=normal.xz*.018*saturate(thickness*.5);
                half3 transmitted=SampleSceneColor(saturate(uv+offset));
                half3 color=lerp(transmitted*tint*1.55,tint,.32+absorb*.26);
                half3 sky=lerp(half3(.22,.55,.64),half3(.61,.78,.79),saturate(view.y*.5+.5));
                color=lerp(color,above?sky:_ShallowColor.rgb,fresnel*.6);
                Light sun=GetMainLight();half spec=pow(saturate(dot(normal,normalize(sun.direction+view))),140);
                color+=sun.color*spec*.5;
                float noise=sin(input.positionWS.x*15+sin(input.positionWS.z*9)+_OceanTime)*.12+.87;
                half foam=above?(1-smoothstep(.02,.35,thickness))*noise:0;
                color=lerp(color,_FoamColor.rgb,foam*.8);
                return half4(MixFog(color,input.fog),saturate(.68+fresnel*.22+foam*.1));
            }
            ENDHLSL
        }
    }
}
