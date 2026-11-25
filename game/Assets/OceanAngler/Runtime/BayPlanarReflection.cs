using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace OceanAngler
{
    // Static views reuse their reflection. The fixed-hierarchy fishing rig can
    // also invalidate it through transforms, rod bones, visibility and line pose.
    [ExecuteAlways,DefaultExecutionOrder(1000),DisallowMultipleComponent] public sealed class BayPlanarReflection : MonoBehaviour
    {
        Camera mirror;RenderTexture target;bool cached;Vector3 lastPosition;Quaternion lastRotation;Matrix4x4 lastProjection;
        Camera preparedSource;bool preparedAbove;float preparedLevel,lastLevel;int lastContent,lastMask;
        Renderer[] subjectRenderers=System.Array.Empty<Renderer>();Transform[] subjectBones=System.Array.Empty<Transform>();LineRenderer[] subjectLines=System.Array.Empty<LineRenderer>();
        readonly UniversalRenderPipeline.SingleCameraRequest request=new UniversalRenderPipeline.SingleCameraRequest();
        public RenderTexture Texture=>target;
        public Matrix4x4 ViewProjection {get;private set;}
        public int RenderCount {get;private set;}
        public bool Ready {get;private set;}
        public Transform TrackedSubject {get;private set;}
        public void TrackSubject(Transform subject)
        {
            if(ReferenceEquals(subject,TrackedSubject))return;
            TrackedSubject=subject;cached=false;
            subjectRenderers=subject==null?System.Array.Empty<Renderer>():subject.GetComponentsInChildren<Renderer>(true);
            var bones=new HashSet<Transform>();var lines=new List<LineRenderer>();
            foreach(var renderer in subjectRenderers)
            {
                if(renderer is SkinnedMeshRenderer skinned)foreach(var bone in skinned.bones)if(bone!=null)bones.Add(bone);
                if(renderer is LineRenderer line)lines.Add(line);
            }
            subjectBones=new Transform[bones.Count];bones.CopyTo(subjectBones);subjectLines=lines.ToArray();
        }
        int ContentSignature()
        {
            unchecked
            {
                int value=17;
                foreach(var renderer in subjectRenderers)if(renderer!=null)
                {
                    value=value*31+renderer.transform.localToWorldMatrix.GetHashCode();
                    value=value*31+(renderer.enabled&&renderer.gameObject.activeInHierarchy?1:0);
                }
                foreach(var bone in subjectBones)if(bone!=null)value=value*31+bone.localToWorldMatrix.GetHashCode();
                foreach(var line in subjectLines)if(line!=null)
                {
                    value=value*31+line.positionCount;value=value*31+(line.useWorldSpace?1:0);
                    value=value*31+line.startWidth.GetHashCode();value=value*31+line.endWidth.GetHashCode();
                    for(int i=0;i<line.positionCount;i++)value=value*31+line.GetPosition(i).GetHashCode();
                }
                return value;
            }
        }
        void LateUpdate()
        {
            // Rig LateUpdate and Animator have now supplied the current pose.
            // The existing bound texture is updated in place before camera rendering.
            if(TrackedSubject!=null&&preparedSource!=null&&preparedAbove)Prepare(preparedSource,true,preparedLevel);
        }
        public static Matrix4x4 MirrorMatrix(float level)
        {var m=Matrix4x4.identity;m.m11=-1;m.m13=2*level;return m;}
        public void Prepare(Camera source,bool above,float level)
        {
            preparedSource=source;preparedAbove=above;preparedLevel=level;
            Ready=false;
            if(!isActiveAndEnabled||!above||SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)return;
            float scale=Mathf.Min(.5f,768f/Mathf.Max(source.pixelWidth,source.pixelHeight));
            int width=Mathf.Max(64,Mathf.RoundToInt(source.pixelWidth*scale)),height=Mathf.Max(64,Mathf.RoundToInt(source.pixelHeight*scale));
            if(target==null||target.width!=width||target.height!=height)
            {
                Release();target=new RenderTexture(width,height,16,RenderTextureFormat.ARGB32){name="BayReflection",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};target.Create();
            }
            int content=ContentSignature();
            if(cached&&lastPosition==source.transform.position&&lastRotation==source.transform.rotation&&lastProjection==source.projectionMatrix&&lastLevel==level&&lastMask==source.cullingMask&&lastContent==content){Ready=true;return;}
            if(mirror==null)
            {
                var go=new GameObject("BayReflectionCamera"){hideFlags=HideFlags.HideAndDontSave};go.transform.SetParent(transform,false);mirror=go.AddComponent<Camera>();mirror.enabled=false;
                var data=go.AddComponent<UniversalAdditionalCameraData>();data.renderShadows=false;data.requiresColorTexture=false;data.requiresDepthTexture=false;data.allowXRRendering=false;
            }
            mirror.CopyFrom(source);mirror.enabled=false;mirror.cameraType=CameraType.Reflection;mirror.allowHDR=false;mirror.allowMSAA=false;mirror.useOcclusionCulling=false;mirror.cullingMask=source.cullingMask&~((1<<4)|(1<<5));
            var reflection=MirrorMatrix(level);mirror.transform.position=reflection.MultiplyPoint(source.transform.position);
            mirror.transform.rotation=Quaternion.LookRotation(Vector3.Reflect(source.transform.forward,Vector3.up),Vector3.Reflect(source.transform.up,Vector3.up));
            mirror.worldToCameraMatrix=source.worldToCameraMatrix*reflection;
            var point=mirror.worldToCameraMatrix.MultiplyPoint(new Vector3(0,level+.035f,0));var normal=mirror.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
            mirror.projectionMatrix=source.CalculateObliqueMatrix(new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(point,normal)));
            request.destination=target;
            if(!RenderPipeline.SupportsRenderRequest(mirror,request))return;
            bool prior=GL.invertCulling;
            try{GL.invertCulling=!prior;RenderPipeline.SubmitRenderRequest(mirror,request);}
            finally{GL.invertCulling=prior;}
            ViewProjection=GL.GetGPUProjectionMatrix(mirror.projectionMatrix,true)*mirror.worldToCameraMatrix;
            lastPosition=source.transform.position;lastRotation=source.transform.rotation;lastProjection=source.projectionMatrix;lastLevel=level;lastMask=source.cullingMask;lastContent=content;cached=Ready=true;RenderCount++;
        }
        void Release()
        {
            cached=Ready=false;
            if(target!=null){target.Release();if(Application.isPlaying)Destroy(target);else DestroyImmediate(target);target=null;}
            if(mirror!=null){if(Application.isPlaying)Destroy(mirror.gameObject);else DestroyImmediate(mirror.gameObject);mirror=null;}
        }
        void OnDisable(){preparedSource=null;preparedAbove=false;Release();}
        void OnDestroy(){Release();}
    }
}
