using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace OceanAngler.Editor
{
    public static class BaySchoolBuilder
    {
        public static void Configure(BayHabitat bay)
        {
            var go=new GameObject("AmbientBluefinSchool");go.transform.SetParent(bay.transform,false);
            var school=go.AddComponent<BayFishSchool>();school.fishPrefab=Resources.Load<GameObject>("Models/Bluefin");
            var horizontal=new Bounds(new Vector3(2,0,13),new Vector3(7,0,10));float padding=BayFishSchool.SafetyRadius+.1f,max=float.NegativeInfinity;
            foreach(var filter in bay.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!filter.name.EndsWith("_LOD0")||filter.name=="Water_LOD0")continue;
                var vertices=filter.sharedMesh.vertices;var triangles=filter.sharedMesh.triangles;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    Vector3 P(int j)=>bay.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[triangles[i+j]]));
                    var a=P(0);var b=P(1);var c=P(2);
                    if(Mathf.Max(a.x,Mathf.Max(b.x,c.x))<horizontal.min.x-padding||Mathf.Min(a.x,Mathf.Min(b.x,c.x))>horizontal.max.x+padding||Mathf.Max(a.z,Mathf.Max(b.z,c.z))<horizontal.min.z-padding||Mathf.Min(a.z,Mathf.Min(b.z,c.z))>horizontal.max.z+padding)continue;
                    max=Mathf.Max(max,Mathf.Max(a.y,Mathf.Max(b.y,c.y)));
                }
            }
            float low=max+padding,high=bay.SeaLevel-padding;
            if(float.IsInfinity(max)||high-low<.25f)throw new InvalidOperationException("No verified free water column for ambient fish: solidTop="+max+" low="+low+" high="+high);
            school.solidCeiling=max;school.centres=new Bounds(new Vector3(horizontal.center.x,(low+high)*.5f,horizontal.center.z),new Vector3(7,high-low,10));bay.school=school;
            Debug.Log("SCHOOL_CLEAR_VOLUME minY="+low+" maxY="+high+" solidTop="+max);
        }
        public static void Prepare()
        {
            const string path="Assets/OceanAngler/Resources/Models/CoralBay.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var bay=root.GetComponent<BayHabitat>();if(bay.school!=null)UnityEngine.Object.DestroyImmediate(bay.school.gameObject);
                Configure(bay);PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
