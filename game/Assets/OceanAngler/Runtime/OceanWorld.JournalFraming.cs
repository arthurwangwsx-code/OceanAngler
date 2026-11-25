using UnityEngine;
namespace OceanAngler
{
    public sealed partial class OceanWorld
    {
        Vector3[] journalBoundsCorners=new Vector3[8];
        Bounds journalGeometryBounds;
        bool journalFitCached;
        Rect journalLastRect;Matrix4x4 journalLastCamera;Quaternion journalLastRotation;
        float journalLastZoom,journalLastDepth,journalFitScale;Vector3 journalFitPosition;
        Vector3 JournalReferencePosition()=>new Vector3(ui.CompactViewer?-1.3f:2.35f,ui.CompactViewer?.7f:1.35f,1.2f);
        Quaternion JournalRotation()=>JournalRotation(ui.ModelYaw);
        Quaternion JournalRotation(float yaw)
        {
            var rotation=Quaternion.Euler(0,-25+yaw,-3);
            return fishId=="halibut"?rotation*Quaternion.Euler(-55,0,0):rotation;
        }
        public int JournalFitCalculations {get;private set;}
        [System.Serializable] public struct JournalFitState
        {public Matrix4x4 camera;public Rect area;public Quaternion rotation;public float zoom,depth;}
        public JournalFitState CurrentJournalFit=>new JournalFitState{camera=journalLastCamera,area=journalLastRect,rotation=journalLastRotation,zoom=journalLastZoom,depth=journalLastDepth};
        void CacheJournalGeometryBounds()
        {
            var envelope=JournalEnvelopeData.Find(fishId);
            if(envelope!=null&&envelope.sections!=null&&envelope.sections.Length>0)
            {
                journalGeometryBounds=envelope.bounds;journalBoundsCorners=new Vector3[envelope.sections.Length*8];int cursor=0;
                foreach(var section in envelope.sections)for(int i=0;i<8;i++)journalBoundsCorners[cursor++]=section.center-journalGeometryBounds.center+Vector3.Scale(section.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                journalFitCached=false;return;
            }
            journalBoundsCorners=new Vector3[8];
            bool first=true;
            foreach(var renderer in fishRoot.GetComponentsInChildren<Renderer>(true))
            {
                // Imported/procedural skinned culling bounds may be deliberately
                // oversized. Use source geometry plus a stable animation margin.
                var bounds=renderer is SkinnedMeshRenderer skin?skin.sharedMesh.bounds:renderer.localBounds;
                for(int i=0;i<8;i++)
                {
                    var p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    p=fishRoot.InverseTransformPoint(renderer.transform.TransformPoint(p));
                    if(first){journalGeometryBounds=new Bounds(p,Vector3.zero);first=false;}else journalGeometryBounds.Encapsulate(p);
                }
            }
            journalGeometryBounds.Expand(new Vector3(.12f,.4f,.2f));
            for(int i=0;i<8;i++)journalBoundsCorners[i]=Vector3.Scale(journalGeometryBounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
            journalFitCached=false;
        }
        void LateUpdate()
        {
            if(game==null||game.Progress.RequiresRecovery||fishRoot==null||view==null||ui.FocusedFish==null||!ui.TryJournalModelRect(out var area))return;
            float padding=4*DisplayMetrics.PixelsPerPoint;area=Rect.MinMaxRect(area.xMin+padding,area.yMin+padding,area.xMax-padding,area.yMax-padding);
            if(area.width<=0||area.height<=0)return;
            // UI events can run after Update. Read this frame's requested pose
            // here too, instead of fitting the previous rendered yaw for one frame.
            var rotation=JournalRotation();var cameraMatrix=view.projectionMatrix*view.worldToCameraMatrix;
            float depth=Mathf.Max(view.nearClipPlane*4,Vector3.Dot(JournalReferencePosition()-view.transform.position,view.transform.forward));
            if(!journalFitCached||area!=journalLastRect||cameraMatrix!=journalLastCamera||rotation!=journalLastRotation||ui.ModelZoom!=journalLastZoom||Mathf.Abs(depth-journalLastDepth)>.0001f)
            {
                var lower=view.transform.InverseTransformPoint(view.ScreenToWorldPoint(new Vector3(area.xMin,area.yMin,1)));
                var upper=view.transform.InverseTransformPoint(view.ScreenToWorldPoint(new Vector3(area.xMax,area.yMax,1)));
                bool Fits(float scale,Quaternion pose,out Vector3 center)
                {
                    float minX=float.NegativeInfinity,maxX=float.PositiveInfinity,minY=minX,maxY=maxX;
                    for(int i=0;i<journalBoundsCorners.Length;i++)
                    {
                        var q=view.transform.InverseTransformVector(pose*(journalBoundsCorners[i]*scale));float z=depth+q.z;
                        if(z<=view.nearClipPlane){center=default;return false;}
                        // Perspective gives an interval for the object's plane
                        // position at each corner. Intersect all intervals, then
                        // center within the feasible space instead of pinning a
                        // 3D bounds center whose projection can be off-center.
                        minX=Mathf.Max(minX,lower.x/lower.z*z-q.x);maxX=Mathf.Min(maxX,upper.x/upper.z*z-q.x);
                        minY=Mathf.Max(minY,lower.y/lower.z*z-q.y);maxY=Mathf.Min(maxY,upper.y/upper.z*z-q.y);
                    }
                    center=view.transform.TransformPoint(new Vector3((minX+maxX)*.5f,(minY+maxY)*.5f,depth));return minX<=maxX&&minY<=maxY;
                }
                float Limit(Quaternion pose)
                {
                    float low=0,high=64;for(int i=0;i<22;i++){float middle=(low+high)*.5f;if(Fits(middle,pose,out _))low=middle;else high=middle;}return low;
                }
                // Preserve the front-view cap and actual sampled animation
                // envelope, four-point UI padding and all existing size gates.
                journalFitScale=Mathf.Min(Limit(rotation),Limit(JournalRotation(0)))*ui.ModelZoom/1.5f;
                if(!Fits(journalFitScale,rotation,out var center))return;
                journalFitPosition=center-rotation*(journalGeometryBounds.center*journalFitScale);
                journalLastRect=area;journalLastCamera=cameraMatrix;journalLastRotation=rotation;journalLastZoom=ui.ModelZoom;journalLastDepth=depth;journalFitCached=true;JournalFitCalculations++;
            }
            fishRoot.SetPositionAndRotation(journalFitPosition,rotation);fishRoot.localScale=Vector3.one*journalFitScale;
        }
    }
}
