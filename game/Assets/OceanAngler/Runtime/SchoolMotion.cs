using System;
using UnityEngine;
namespace OceanAngler
{
    // Small, bounded steering model. Not a biological population simulation.
    public sealed class SchoolMotion
    {
        public const int Count=3;
        readonly Bounds bounds;
        readonly Vector3[] positions=new Vector3[Count],velocities=new Vector3[Count],next=new Vector3[Count];
        float time,accumulator;
        public int BoundaryCorrections {get;private set;}
        public int NeighbourInteractions {get;private set;}
        public int SeparationResponses {get;private set;}
        public SchoolMotion(Bounds volume)
        {
            for(int i=0;i<3;i++)if(float.IsNaN(volume.center[i])||float.IsInfinity(volume.center[i])||float.IsNaN(volume.size[i])||float.IsInfinity(volume.size[i]))throw new ArgumentException("Non-finite school volume");
            if(volume.size.x<5||volume.size.z<7||volume.size.y<.25f)throw new ArgumentException("School needs a clear swimming volume");
            bounds=volume;
            for(int i=0;i<Count;i++){positions[i]=Goal(i,0);velocities[i]=new Vector3(-Mathf.Sin(i*.4f),0,Mathf.Cos(i*.4f))*.4f;}
        }
        public Vector3 Position(int i)=>positions[i];
        public Vector3 Velocity(int i)=>velocities[i];
        Vector3 Goal(int i,float t)
        {
            float a=t*.13f+i*.4f;
            return bounds.center+new Vector3(Mathf.Cos(a)*(bounds.extents.x-.9f),Mathf.Sin(t*.17f+i)*Mathf.Min(.1f,bounds.extents.y*.45f),Mathf.Sin(a)*(bounds.extents.z-1.2f));
        }
        public void Advance(float delta,bool paused,bool reduced)
        {
            if(paused||reduced||float.IsNaN(delta)||float.IsInfinity(delta)||delta<=0)return;
            accumulator+=Mathf.Min(delta,.1f);
            while(accumulator>=1f/30){Step(1f/30);accumulator-=1f/30;}
        }
        void Step(float dt)
        {
            time+=dt;
            for(int i=0;i<Count;i++)
            {
                Vector3 alignment=Vector3.zero,cohesion=Vector3.zero,separation=Vector3.zero;int neighbours=0;
                for(int j=0;j<Count;j++)if(j!=i)
                {
                    var away=positions[i]-positions[j];float d=away.magnitude;
                    if(d<4){alignment+=velocities[j];cohesion+=positions[j];neighbours++;NeighbourInteractions++;}
                    if(d<1.4f){separation+=away/Mathf.Max(.05f,d*d);SeparationResponses++;}
                }
                var desired=(Goal(i,time+1.2f)-positions[i]).normalized*.48f;
                if(neighbours>0)desired+=(alignment/neighbours-velocities[i])*.12f+(cohesion/neighbours-positions[i])*.025f;
                desired+=separation*.6f;desired.y=Mathf.Clamp(desired.y,-.08f,.08f);
                var inner=bounds;inner.Expand(new Vector3(-.5f,-Mathf.Min(.1f,bounds.size.y*.2f),-.5f));
                desired+=(inner.ClosestPoint(positions[i])-positions[i])*1.5f;
                desired=Vector3.ClampMagnitude(desired,.65f);
                next[i]=Vector3.RotateTowards(velocities[i],desired,1.1f*dt,.35f*dt);
            }
            for(int i=0;i<Count;i++)
            {
                velocities[i]=next[i];var candidate=positions[i]+velocities[i]*dt;
                if(!bounds.Contains(candidate)){candidate=bounds.ClosestPoint(candidate);BoundaryCorrections++;}
                positions[i]=candidate;
            }
        }
    }
}
