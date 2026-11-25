using UnityEngine;
namespace OceanAngler
{
    // Explicit, non-fishing observation visit; never enters the catch selector.
    public sealed class ObservationEncounter : MonoBehaviour
    {
        public string SpeciesId {get;private set;}
        public ObservationVisual Visual {get;private set;}
        public bool Playing {get;private set;}
        public bool Visible {get;private set;}
        public float ViewedSeconds {get;private set;}
        public bool CanRecord=>Visual!=null&&Visual.gameObject.activeSelf&&Visible&&Foreground&&ViewedSeconds>=2;
        bool focused=true,background;float time;
        bool Foreground=>focused&&!background;
        public void Toggle(){if(Visual!=null&&Visual.gameObject.activeSelf&&Foreground)Playing=!Playing;}
        public void Present(string id,Camera camera,bool compact,bool reduced,float delta)
        {
            if(id==null){if(Visual!=null)Visual.gameObject.SetActive(false);Playing=false;ViewedSeconds=0;Visible=false;return;}
            if(SpeciesId!=id)
            {
                if(Visual!=null){Visual.gameObject.SetActive(false);if(Application.isPlaying)Destroy(Visual.gameObject);else DestroyImmediate(Visual.gameObject);}
                var root=new GameObject("Observation-"+id);root.transform.SetParent(transform,false);Visual=root.AddComponent<ObservationVisual>();Visual.Initialize(id);
                SpeciesId=id;Playing=false;ViewedSeconds=0;time=0;
            }
            Visual.gameObject.SetActive(true);
            if(Playing&&Foreground){float step=Mathf.Clamp(delta,0,.25f);time+=step;if(Visible)ViewedSeconds+=step;}
            var target=new Vector3(0,.9f,1.2f);
            Visual.transform.position=target+new Vector3(reduced?0:Mathf.Sin(time*.3f)*.15f,reduced?0:Mathf.Sin(time*.5f)*.06f,0);
            Visual.transform.rotation=Quaternion.Euler(0,-12,0);Visual.Pose(time,reduced);
            camera.transform.SetPositionAndRotation(new Vector3(0,1.1f,-5.5f),Quaternion.LookRotation(new Vector3(0,.85f,2.2f)-new Vector3(0,1.1f,-5.5f)));
            camera.fieldOfView=42;camera.clearFlags=CameraClearFlags.SolidColor;
            var viewport=camera.WorldToViewportPoint(Visual.transform.position);Visible=viewport.z>camera.nearClipPlane&&viewport.z<camera.farClipPlane&&viewport.x>.05f&&viewport.x<.95f&&viewport.y>.12f&&viewport.y<.82f;
        }
        void OnApplicationFocus(bool focus){focused=focus;if(!focus){Playing=false;ViewedSeconds=0;}}
        void OnApplicationPause(bool paused){background=paused;if(paused){Playing=false;ViewedSeconds=0;}}
        void OnDisable(){Playing=false;ViewedSeconds=0;Visible=false;}
    }
}
