using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace OceanAngler
{
    // The bay is static while inspected. Capture only on a view/size change,
    // never once per water-wave frame, and never from the underwater camera.
    [ExecuteAlways,DisallowMultipleComponent] public sealed class BayPlanarReflection : MonoBehaviour
    {
        Camera mirror;RenderTexture target;bool cached;Vector3 lastPosition;Quaternion lastRotation;Matrix4x4 lastProjection;
        readonly UniversalRenderPipeline.SingleCameraRequest request=new UniversalRenderPipeline.SingleCameraRequest();
        public RenderTexture Texture=>target;
        public Matrix4x4 ViewProjection {get;private set;}
        public int RenderCount {get;private set;}
        public bool Ready {get;private set;}
        public static Matrix4x4 MirrorMatrix(float level)
        {var m=Matrix4x4.identity;m.m11=-1;m.m13=2*level;return m;}
        public void Prepare(Camera source,bool above,float level)
        {
            Ready=false;
            if(!isActiveAndEnabled||!above||SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)return;
            float scale=Mathf.Min(.5f,768f/Mathf.Max(source.pixelWidth,source.pixelHeight));
            int width=Mathf.Max(64,Mathf.RoundToInt(source.pixelWidth*scale)),height=Mathf.Max(64,Mathf.RoundToInt(source.pixelHeight*scale));
            if(target==null||target.width!=width||target.height!=height)
            {
                Release();target=new RenderTexture(width,height,16,RenderTextureFormat.ARGB32){name="BayReflection",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};target.Create();
            }
            if(cached&&lastPosition==source.transform.position&&lastRotation==source.transform.rotation&&lastProjection==source.projectionMatrix){Ready=true;return;}
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
            lastPosition=source.transform.position;lastRotation=source.transform.rotation;lastProjection=source.projectionMatrix;cached=Ready=true;RenderCount++;
        }
        void Release()
        {
            cached=Ready=false;
            if(target!=null){target.Release();if(Application.isPlaying)Destroy(target);else DestroyImmediate(target);target=null;}
            if(mirror!=null){if(Application.isPlaying)Destroy(mirror.gameObject);else DestroyImmediate(mirror.gameObject);mirror=null;}
        }
        void OnDisable(){Release();}
        void OnDestroy(){Release();}
    }
}
