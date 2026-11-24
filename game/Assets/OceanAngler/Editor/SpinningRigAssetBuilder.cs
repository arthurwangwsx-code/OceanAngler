using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace OceanAngler.Editor
{
    public static class SpinningRigAssetBuilder
    {
        const string Folder="Assets/OceanAngler/Art/SpinningRig";
        static readonly string[] Parts={"Rod","Housing","Rotor","Spool","Crank","Lure","BailWire","LineRoller"};
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Folder,"*.png"))
            {
                var ti=(TextureImporter)AssetImporter.GetAtPath(path);bool normal=path.Contains("_Normal");ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=!normal;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.maxTextureSize=1024;ti.SaveAndReimport();
            }
            var materials=new[]{Create("Carbon",Color.white,.15f,.70f,true),Create("Cork",Color.white,0,.35f,true),Create("Metal",new Color(.56f,.64f,.67f),.82f,.64f),Create("Black",new Color(.025f,.033f,.037f),.15f,.58f),Create("Accent",new Color(.13f,.42f,.41f),.65f,.67f),Create("Line",new Color(.59f,.68f,.43f),0,.45f),Create("Lure",new Color(.23f,.63f,.65f),.25f,.77f)};
            var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/SpinningRig.fbx");importer.importAnimation=false;importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importVisibility=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
            foreach(var material in materials)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),material.name),material);
            importer.SaveAndReimport();
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/SpinningRig.fbx");var root=new GameObject("SpinningRigCandidate");var align=new GameObject("CoordinateAlignment").transform;align.SetParent(root.transform,false);var imported=(GameObject)PrefabUtility.InstantiatePrefab(source);imported.transform.SetParent(align,false);PrefabUtility.UnpackPrefabInstance(imported,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            try
            {
                Transform Find(string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
                Vector3 origin=Find("ButtAnchor").position,up=(Find("TipAnchor").position-origin).normalized;var forward=Vector3.ProjectOnPlane(Find("FrontAnchor").position-origin,up).normalized;
                align.rotation=Quaternion.LookRotation(Vector3.back,Vector3.up)*Quaternion.Inverse(Quaternion.LookRotation(forward,up));align.position=-Find("ButtAnchor").position;
                foreach(var g in imported.GetComponentsInChildren<LODGroup>(true))UnityEngine.Object.DestroyImmediate(g);
                foreach(var a in imported.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);
                var renderers=root.GetComponentsInChildren<Renderer>(true);var lods=new LOD[3];
                for(int i=0;i<3;i++)
                {
                    int level=i;var levelRenderers=renderers.Where(r=>Parts.Any(p=>r.name==p+"_LOD"+level)).ToArray();
                    if(levelRenderers.Length!=Parts.Length)throw new InvalidOperationException("Missing spinning rig LOD assemblies");
                    foreach(var r in levelRenderers){r.gameObject.SetActive(true);r.enabled=true;}
                    lods[i]=new LOD(i==0?.12f:i==1?.04f:.012f,levelRenderers);
                }
                var group=root.AddComponent<LODGroup>();group.SetLODs(lods);group.RecalculateBounds();
                var rig=root.AddComponent<AuthoredSpinningRig>();rig.rodBones=Enumerable.Range(0,10).Select(i=>Find("RodBone"+i)).ToArray();rig.guides=Enumerable.Range(0,9).Select(i=>Find("Guide"+i)).ToArray();
                rig.rotor=Find("RotorPivot");rig.rotorAxis=Find("RotorAxis");rig.spool=Find("SpoolPivot");rig.crank=Find("CrankPivot");rig.crankAxis=Find("CrankAxis");rig.bail=Find("BailPivot");rig.bailAxis=Find("BailAxis");rig.lineExit=Find("LineExit");rig.lure=Find("LurePivot");rig.lureNose=Find("LureNose");rig.inspectionCenter=root.transform.InverseTransformPoint((rig.spool.position+rig.crank.position)*.5f);
                rig.roller=Find("LineRollerPivot");rig.rollerAxis=Find("LineRollerAxis");rig.rollerSurface=Find("RollerSurface");rig.spoolLineRadius=Find("SpoolLineRadius");rig.spoolLipCenter=Find("SpoolLipCenter");rig.spoolLipSurface=Find("SpoolLipSurface");
                var line=new GameObject("WorkingFishingLine").AddComponent<LineRenderer>();line.transform.SetParent(root.transform,false);line.useWorldSpace=true;line.sharedMaterial=materials.Single(m=>m.name=="Rig_Line");line.startWidth=.002f;line.endWidth=.0013f;line.numCornerVertices=2;line.generateLightingData=true;rig.fishingLine=line;
                var collision=root.AddComponent<CapsuleCollider>();collision.direction=1;collision.center=new Vector3(0,1.16f,0);collision.height=2.35f;collision.radius=.015f;collision.isTrigger=true;
                if(!AssetDatabase.IsValidFolder("Assets/OceanAngler/Resources/Models"))AssetDatabase.CreateFolder("Assets/OceanAngler/Resources","Models");
                if(Vector3.Distance(Find("ButtAnchor").position,Vector3.zero)>.001f||Vector3.Distance(Find("TipAnchor").position,new Vector3(0,2.35f,0))>.001f)throw new InvalidOperationException("Rig coordinate alignment failed");
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/OceanAngler/Resources/Models/SpinningRig.prefab");Debug.Log("SPINNING_RIG_UNITY_OK lods=3 assemblies="+Parts.Length+" guides="+rig.guides.Length);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            AssetDatabase.SaveAssets();
        }
        static Material Create(string name,Color color,float metallic,float smooth,bool textured=false)
        {
            string path=Folder+"/Rig_"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.color=color;material.SetColor("_BaseColor",color);material.SetColor("_Color",color);material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",smooth);
            if(textured){material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Rig_"+name+".png"));material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Rig_"+name+"_Normal.png"));material.SetFloat("_BumpScale",.5f);material.EnableKeyword("_NORMALMAP");}
            EditorUtility.SetDirty(material);return material;
        }
    }
}
