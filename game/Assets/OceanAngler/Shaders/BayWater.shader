Shader "OceanAngler/BayWater"
{
    Properties
    {
        _ShallowColor("Shallow water",Color)=(0.12,0.54,0.57,1)
        _DeepColor("Deep water",Color)=(0.025,0.20,0.27,1)
        _FoamColor("Shore foam",Color)=(0.72,0.87,0.82,1)
        _OceanTime("Water time",Float)=0
        _SeaLevel("Sea level",Float)=4.5
        _HasReflection("Reflection ready",Float)=0
        _FishingSplash("Fishing splash x z age strength",Vector)=(0,0,-1,0)
        _PlanarReflection("Planar reflection",2D)="black"{}
        _BoatOrigin("Static dinghy origin and rim",Vector)=(-2.5,4.5,3.8,.48)
        _Boat0("Hull station 0",Vector)=(-1.8,.03,-.02,0)
        _Boat1("Hull station 1",Vector)=(-1.45,.38,-.20,0)
        _Boat2("Hull station 2",Vector)=(-.8,.61,-.34,0)
        _Boat3("Hull station 3",Vector)=(0,.66,-.38,0)
        _Boat4("Hull station 4",Vector)=(.8,.57,-.34,0)
        _Boat5("Hull station 5",Vector)=(1.45,.38,-.20,0)
        _Boat6("Hull station 6",Vector)=(1.8,.04,-.02,0)
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
            float4 _BoatOrigin,_Boat0,_Boat1,_Boat2,_Boat3,_Boat4,_Boat5,_Boat6;
            float4 _FishingSplash;
            float _OceanTime,_SeaLevel,_HasReflection;
            float4x4 _ReflectionVP;
            CBUFFER_END
            TEXTURE2D(_PlanarReflection);SAMPLER(sampler_PlanarReflection);
            struct Attributes {float4 positionOS:POSITION;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;half fog:TEXCOORD2;};
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world=TransformObjectToWorld(input.positionOS.xyz);
                float a=world.x*.34+world.z*.21+_OceanTime*.65;
                float b=world.z*.48-world.x*.14-_OceanTime*.83;
                world.y+=sin(a)*.055+sin(b)*.025;
                float dx=cos(a)*.055*.34-cos(b)*.025*.14;
                float dz=cos(a)*.055*.21+cos(b)*.025*.48;
                output.positionWS=world;output.positionCS=TransformWorldToHClip(world);output.normalWS=normalize(float3(-dx,1,-dz));output.fog=ComputeFogFactor(output.positionCS.z);return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                // Exclude water inside this static hull using the same source
                // cross sections as the authored dinghy, not a bounding ellipse.
                float3 local=input.positionWS-_BoatOrigin.xyz;
                UNITY_BRANCH if(abs(local.x)<.7&&local.z>_Boat0.x&&local.z<_Boat6.x)
                {
                    float4 stations[7]={_Boat0,_Boat1,_Boat2,_Boat3,_Boat4,_Boat5,_Boat6};
                    UNITY_UNROLL for(int i=0;i<6;i++)if(local.z>=stations[i].x&&local.z<stations[i+1].x)
                    {
                        float t=(local.z-stations[i].x)/(stations[i+1].x-stations[i].x);float width=lerp(stations[i].y,stations[i+1].y,t),keel=lerp(stations[i].z,stations[i+1].z,t);
                        float inside=keel+(_BoatOrigin.w-keel)*pow(saturate(abs(local.x)/width),1.5)+.035;
                        if(abs(local.x)<width-.02&&local.y>inside)discard;
                    }
                }
                float2 uv=GetNormalizedScreenSpaceUV(input.positionCS);
                float rawDepth=SampleSceneDepth(uv);
                float depth=LinearEyeDepth(rawDepth,_ZBufferParams);
                #if !UNITY_REVERSED_Z
                rawDepth=lerp(UNITY_NEAR_CLIP_VALUE,1,rawDepth);
                #endif
                float3 bedPosition=ComputeWorldSpacePosition(uv,rawDepth,UNITY_MATRIX_I_VP);
                float eyeDepth=-TransformWorldToView(input.positionWS).z;
                float thickness=max(0,depth-eyeDepth);
                float3 view=normalize(GetWorldSpaceViewDir(input.positionWS));
                float a=input.positionWS.x*.34+input.positionWS.z*.21+_OceanTime*.65;
                float b=input.positionWS.z*.48-input.positionWS.x*.14-_OceanTime*.83;
                float c=input.positionWS.x*5.3+input.positionWS.z*2.7+_OceanTime*1.4;
                float d=input.positionWS.z*4.5-input.positionWS.x*1.8-_OceanTime*1.1;
                float footprint=max(length(ddx(input.positionWS.xz)),length(ddy(input.positionWS.xz)));
                float micro=1-smoothstep(.06,.25,footprint);
                float dx=cos(a)*.055*.34-cos(b)*.025*.14+micro*(cos(c)*.008*5.3-cos(d)*.006*1.8);
                float dz=cos(a)*.055*.21+cos(b)*.025*.48+micro*(cos(c)*.008*2.7+cos(d)*.006*4.5);
                float2 splashDelta=input.positionWS.xz-_FishingSplash.xy;
                float splashDistance=length(splashDelta),splashAge=max(0,_FishingSplash.z);
                float ringDistance=splashDistance-splashAge*1.4;
                float splashEnvelope=_FishingSplash.w*exp(-splashAge*2-abs(ringDistance)*5)*(1-smoothstep(1,1.4,splashAge));
                float rippleSlope=cos(ringDistance*22)*.05*splashEnvelope;
                float2 rippleDirection=splashDelta/max(.001,splashDistance);
                dx+=rippleDirection.x*rippleSlope;dz+=rippleDirection.y*rippleSlope;
                half3 normal=normalize(float3(-dx,1,-dz));bool above=_WorldSpaceCameraPos.y>_SeaLevel;
                if(!above)normal=-normal;
                half fresnel=.02+.98*pow(1-saturate(dot(view,normal)),5);
                // The scene copy already contains the background; output its final
                // attenuated composite once rather than alpha-blending it twice.
                float2 offset=normal.xz*.018*saturate(thickness*.6);
                if(LinearEyeDepth(SampleSceneDepth(saturate(uv+offset)),_ZBufferParams)<eyeDepth+.02)offset=0;
                half3 transmitted=SampleSceneColor(saturate(uv+offset));
                half3 attenuation=exp(-thickness*half3(.30,.16,.095));
                half3 color=transmitted*attenuation+_DeepColor.rgb*(1-attenuation);
                half3 reflected=lerp(half3(.32,.55,.68),half3(.72,.82,.84),saturate(reflect(-view,normal).y));
                if(above&&_HasReflection>.5)
                {
                    float4 projected=mul(_ReflectionVP,float4(input.positionWS,1));float2 reflectUV=projected.xy/projected.w*.5+.5;
                    #if UNITY_UV_STARTS_AT_TOP
                    reflectUV.y=1-reflectUV.y;
                    #endif
                    reflected=SAMPLE_TEXTURE2D(_PlanarReflection,sampler_PlanarReflection,saturate(reflectUV+normal.xz*.012)).rgb;
                }
                color=lerp(color,above?reflected:_ShallowColor.rgb,fresnel);
                Light sun=GetMainLight();half spec=pow(saturate(dot(normal,normalize(sun.direction+view))),180);
                color+=sun.color*spec*.7;
                float noise=sin(input.positionWS.x*15+sin(input.positionWS.z*9)+_OceanTime)*.12+.87;
                half foam=above?(1-smoothstep(.025,.22,max(0,_SeaLevel-bedPosition.y)))*noise:0;
                color=lerp(color,_FoamColor.rgb,foam*.8);
                color=lerp(color,_FoamColor.rgb,exp(-ringDistance*ringDistance*140)*splashEnvelope*.4);
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
    }
}
