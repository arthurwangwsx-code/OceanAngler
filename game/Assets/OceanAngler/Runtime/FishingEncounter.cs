using UnityEngine;
namespace OceanAngler
{
    // An authored presentation path, not fish perception AI or fishing physics.
    public static class FishingEncounter
    {
        public readonly struct Pose
        {
            public readonly Vector3 fishPosition,lurePosition;
            public readonly Quaternion fishRotation;
            public Pose(Vector3 fish,Quaternion rotation,Vector3 lure){fishPosition=fish;fishRotation=rotation;lurePosition=lure;}
        }
        public static Pose Evaluate(FishingPhase phase,float progress,float power,Vector3 hookLocal,Vector3 launch,float lane,float time,bool reduced)
        {
            float u=Mathf.Clamp01(progress),smooth=u*u*(3-2*u);
            var rotation=Quaternion.Euler(0,-25,-3);
            var home=new Vector3(lane,.3f,1.2f);
            var destination=new Vector3(lane-.1f,.2f,1.2f+1.6f*Mathf.Clamp(power,.3f,1));
            var bite=destination-rotation*hookLocal;
            // Keep the large target ahead of the small ambient school and above
            // the rear coral before descending into the clear foreground.
            var start=bite+new Vector3(3,1.4f,2.5f);
            var control=bite-rotation*Vector3.left*2+Vector3.up*.8f;
            var fish=home;var lure=destination;
            if(!reduced)
            {
                switch(phase)
                {
                    case FishingPhase.Casting:
                        fish=start;
                        var castHeading=control-start;
                        rotation=Quaternion.Euler(0,Mathf.Atan2(castHeading.z,-castHeading.x)*Mathf.Rad2Deg,-3);
                        lure=Vector3.Lerp(launch,destination,u)+Vector3.up*(4*u*(1-u)*1.3f);
                        break;
                    case FishingPhase.Waiting:
                        // Quadratic approach ends along the fish's nose direction.
                        float v=1-smooth;
                        fish=v*v*start+2*v*smooth*control+smooth*smooth*bite;
                        var heading=2*v*(control-start)+2*smooth*(bite-control);
                        rotation=Quaternion.Euler(0,Mathf.Atan2(heading.z,-heading.x)*Mathf.Rad2Deg,-3);
                        break;
                    case FishingPhase.Bite: fish=bite;break;
                    case FishingPhase.Fighting:
                        float sweep=Mathf.Sin(u*Mathf.PI);
                        fish=Vector3.Lerp(bite,home,smooth)+new Vector3(Mathf.Sin(time*2.1f)*.22f,Mathf.Sin(time*1.7f)*.08f,0)*sweep;
                        rotation=Quaternion.Euler(0,-25+Mathf.Sin(time*1.6f)*7*sweep,-3);
                        break;
                    case FishingPhase.Landed:fish=home;break;
                    default:fish=home+new Vector3(Mathf.Sin(time*.3f)*.18f,Mathf.Sin(time*.5f)*.05f,0);break;
                }
            }
            return new Pose(fish,rotation,lure);
        }
    }
}
