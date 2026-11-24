Shader "OceanAngler/CoastalTerrain"
{
    Properties
    {
        _BaseMap("Rock colour",2D)="white"{}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _BumpMap("Rock normal",2D)="bump"{}
        _MetallicGlossMap("Rock metallic/smoothness",2D)="black"{}
        _SoilMap("Soil colour",2D)="white"{}
        _SoilNormal("Soil normal",2D)="bump"{}
        _SoilMetallicGlossMap("Soil metallic/smoothness",2D)="black"{}
        _BumpScale("Normal strength",Range(0,1))=.35
        _TextureScale("World texture scale",Float)=.22
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
        CBUFFER_START(UnityPerMaterial)
        half4 _BaseColor;
        half _BumpScale,_TextureScale;
        CBUFFER_END
        TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
        TEXTURE2D(_MetallicGlossMap);SAMPLER(sampler_MetallicGlossMap);
        TEXTURE2D(_SoilMap);SAMPLER(sampler_SoilMap);
        TEXTURE2D(_SoilNormal);SAMPLER(sampler_SoilNormal);
        TEXTURE2D(_SoilMetallicGlossMap);SAMPLER(sampler_SoilMetallicGlossMap);
        struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;half4 color:COLOR;};
        struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half4 fogAndLight:TEXCOORD2;half soil:TEXCOORD3;};
        Varyings TerrainVert(Attributes input)
        {
            Varyings o;
            o.positionWS=TransformObjectToWorld(input.positionOS.xyz);
            o.positionCS=TransformWorldToHClip(o.positionWS);
            o.normalWS=TransformObjectToWorldNormal(input.normalOS);
            o.fogAndLight=half4(VertexLighting(o.positionWS,o.normalWS),ComputeFogFactor(o.positionCS.z));
            o.soil=saturate(input.color.r);
            return o;
        }
        void SampleLayer(float2 uv,half blend,out half3 color,out half3 normal,out half4 packed)
        {
            color=lerp(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb,SAMPLE_TEXTURE2D(_SoilMap,sampler_SoilMap,uv).rgb,blend);
            normal=normalize(lerp(UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uv),_BumpScale),UnpackNormalScale(SAMPLE_TEXTURE2D(_SoilNormal,sampler_SoilNormal,uv),_BumpScale),blend));
            packed=lerp(SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,uv),SAMPLE_TEXTURE2D(_SoilMetallicGlossMap,sampler_SoilMetallicGlossMap,uv),blend);
        }
        float3 ProjectNormal(float3 sampleNormal,float3 n,float3 axisU,float3 axisV)
        {
            float3 t=axisU-n*dot(axisU,n);t*=rsqrt(max(dot(t,t),1e-8));float3 b=cross(n,t);b*=dot(b,axisV)<0?-1:1;
            return normalize(t*sampleNormal.x+b*sampleNormal.y+n*sampleNormal.z);
        }
        half4 TerrainFrag(Varyings input):SV_Target
        {
            half3 n=normalize(input.normalWS),weights=abs(n);weights*=weights;weights*=weights;weights/=weights.x+weights.y+weights.z;
            float3 uv=input.positionWS*_TextureScale;half blend=saturate(input.soil);
            half3 cx,cy,cz,nx,ny,nz;half4 px,py,pz;
            SampleLayer(uv.zy,blend,cx,nx,px);SampleLayer(uv.xz,blend,cy,ny,py);SampleLayer(uv.xy,blend,cz,nz,pz);
            half4 packed=px*weights.x+py*weights.y+pz*weights.z;
            InputData data=(InputData)0;
            data.positionWS=input.positionWS;
            data.normalWS=NormalizeNormalPerPixel(ProjectNormal(nx,n,half3(0,0,1),half3(0,1,0))*weights.x+ProjectNormal(ny,n,half3(1,0,0),half3(0,0,1))*weights.y+ProjectNormal(nz,n,half3(1,0,0),half3(0,1,0))*weights.z);
            data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(input.positionWS);
            data.shadowCoord=TransformWorldToShadowCoord(input.positionWS);
            data.bakedGI=SampleSH(data.normalWS);
            data.vertexLighting=input.fogAndLight.xyz;
            data.shadowMask=half4(1,1,1,1);
            data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);
            SurfaceData surface=(SurfaceData)0;
            surface.albedo=(cx*weights.x+cy*weights.y+cz*weights.z)*_BaseColor.rgb;
            surface.normalTS=half3(0,0,1);surface.metallic=packed.r;surface.smoothness=packed.a;
            surface.specular=half3(.04,.04,.04);surface.occlusion=1;surface.alpha=1;
            half4 color=UniversalFragmentPBR(data,surface);
            color.rgb=MixFog(color.rgb,input.fogAndLight.w);
            return color;
        }
        float3 _LightDirection;
        float3 _LightPosition;
        float4 ShadowVert(Attributes input):SV_POSITION
        {
            float3 p=TransformObjectToWorld(input.positionOS.xyz),n=TransformObjectToWorldNormal(input.normalOS);
            #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
            float3 direction=normalize(_LightPosition-p);
            #else
            float3 direction=_LightDirection;
            #endif
            float4 clip=TransformWorldToHClip(ApplyShadowBias(p,n,direction));
            #if UNITY_REVERSED_Z
            clip.z=min(clip.z,clip.w*UNITY_NEAR_CLIP_VALUE);
            #else
            clip.z=max(clip.z,clip.w*UNITY_NEAR_CLIP_VALUE);
            #endif
            return clip;
        }
        float4 DepthVert(Attributes input):SV_POSITION{return TransformObjectToHClip(input.positionOS.xyz);}
        half4 DepthFrag():SV_Target{return 0;}
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForward"}
            ZWrite On Cull Back
            HLSLPROGRAM
            #pragma vertex TerrainVert
            #pragma fragment TerrainFrag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}
            ZWrite On ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            ENDHLSL
        }
    }
}
