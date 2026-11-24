using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace OceanAngler.Editor
{
    public static class BluefinAssetBuilder
    {
        const string Folder="Assets/OceanAngler/Art/Bluefin";
        static readonly string[] Semantics={"idle","swim","turn","burst","bite","hooked","release"};
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            foreach(string file in Directory.GetFiles(Folder,"*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(file);
                bool normal=file.Contains("_Normal"),linear=normal||file.Contains("_Roughness")||file.Contains("_MetallicSmoothness");
                importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=!linear;importer.alphaIsTransparency=false;importer.maxTextureSize=2048;
                if(Path.GetFileName(file).StartsWith("Bluefin_Fin")||file.EndsWith("Bluefin_Iris.png"))importer.wrapMode=TextureWrapMode.Clamp;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            }
            var skin=CreateMaterial("Bluefin_Skin",Color.white,.65f);
            skin.SetTexture("_BaseMap",Texture("Bluefin_BaseColor"));skin.SetTexture("_BumpMap",Texture("Bluefin_Normal"));skin.SetFloat("_BumpScale",.35f);skin.EnableKeyword("_NORMALMAP");
            skin.SetTexture("_MetallicGlossMap",Texture("Bluefin_MetallicSmoothness"));skin.SetFloat("_Smoothness",1);skin.EnableKeyword("_METALLICSPECGLOSSMAP");
            var fins=CreateMaterial("Bluefin_Fins",Color.white,.50f);fins.SetTexture("_BaseMap",Texture("Bluefin_Fin"));
            fins.SetTexture("_BumpMap",Texture("Bluefin_Fin_Normal"));fins.SetFloat("_BumpScale",.6f);fins.EnableKeyword("_NORMALMAP");fins.SetTexture("_MetallicGlossMap",Texture("Bluefin_Fin_MetallicSmoothness"));fins.SetFloat("_Smoothness",1);fins.EnableKeyword("_METALLICSPECGLOSSMAP");
            fins.SetFloat("_Surface",1);fins.SetFloat("_Blend",0);fins.SetFloat("_BlendModePreserveSpecular",1);fins.SetFloat("_AlphaClip",0);BaseShaderGUI.SetupMaterialBlendMode(fins);fins.SetShaderPassEnabled("ShadowCaster",false);fins.SetShaderPassEnabled("DepthOnly",false);
            var rays=CreateMaterial("Bluefin_Rays",Color.white,.52f);rays.SetTexture("_BaseMap",Texture("Bluefin_Fin"));rays.SetFloat("_Surface",0);rays.SetFloat("_AlphaClip",0);BaseShaderGUI.SetupMaterialBlendMode(rays);rays.SetShaderPassEnabled("ShadowCaster",true);rays.SetShaderPassEnabled("DepthOnly",true);
            var gill=CreateMaterial("Bluefin_Gill",new Color(.10f,.18f,.17f),.42f);
            var oral=CreateMaterial("Bluefin_Oral",new Color(.065f,.043f,.037f),.22f);
            var iris=CreateMaterial("Bluefin_Iris",Color.white,.60f);iris.SetTexture("_BaseMap",Texture("Bluefin_Iris"));
            var dark=CreateMaterial("Bluefin_Dark",new Color(.009f,.023f,.029f),.77f);
            foreach(var material in new[]{skin,fins,rays,gill,oral,iris,dark})EditorUtility.SetDirty(material);
            var model=(ModelImporter)AssetImporter.GetAtPath(Folder+"/Bluefin.fbx");
            model.animationType=ModelImporterAnimationType.Generic;model.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            model.importAnimation=true;model.importVisibility=false;model.importBlendShapes=false;
            model.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var material in new[]{skin,fins,rays,gill,oral,iris,dark})model.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),material.name),material);
            model.SaveAndReimport();
            var definitions=model.defaultClipAnimations;
            foreach(var clip in definitions)clip.loopTime=clip.name!="release"&&!clip.name.EndsWith("|release");
            model.clipAnimations=definitions;model.SaveAndReimport();
            var clips=AssetDatabase.LoadAllAssetsAtPath(Folder+"/Bluefin.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            string controllerPath=Folder+"/Bluefin.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var stateMachine=controller.layers[0].stateMachine;
            foreach(string semantic in Semantics)
            {
                var clip=clips.SingleOrDefault(c=>c.name==semantic||c.name.EndsWith("|"+semantic));
                if(clip==null)throw new InvalidOperationException("Missing imported motion "+semantic+"; found "+string.Join(",",clips.Select(c=>c.name)));
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=semantic!="release";AnimationUtility.SetAnimationClipSettings(clip,settings);
                var state=stateMachine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==semantic)??stateMachine.AddState(semantic);
                state.motion=clip;if(semantic=="swim")stateMachine.defaultState=state;
            }
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Bluefin.fbx");var root=new GameObject("BluefinCandidate");
            var alignment=new GameObject("CoordinateAlignment");alignment.transform.SetParent(root.transform,false);alignment.transform.localRotation=Quaternion.Euler(0,180,0);
            var imported=(GameObject)PrefabUtility.InstantiatePrefab(source);imported.transform.SetParent(alignment.transform,false);
            PrefabUtility.UnpackPrefabInstance(imported,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            foreach(var generatedGroup in imported.GetComponentsInChildren<LODGroup>(true))UnityEngine.Object.DestroyImmediate(generatedGroup);
            try
            {
                var animator=imported.GetComponent<Animator>();if(animator==null)animator=imported.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
                var renderers=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);var lods=new LOD[3];
                for(int i=0;i<3;i++)
                {
                    var renderer=renderers.Single(r=>r.name=="Bluefin_LOD"+i);renderer.enabled=true;renderer.gameObject.SetActive(true);
                    // The group's bounding size is its longest dimension.
                    // Reserve complete internal cylinders for close views;
                    // distance meshes retain opaque outline rays and fin PBR.
                    renderer.updateWhenOffscreen=false;lods[i]=new LOD(i==0?.70f:i==1?.16f:.008f,new Renderer[]{renderer});
                }
                var group=root.GetComponent<LODGroup>();if(group==null)group=root.AddComponent<LODGroup>();group.SetLODs(lods);group.RecalculateBounds();
                var visual=root.AddComponent<AuthoredFishVisual>();
                var jaw=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="jaw");
                var anchor=new GameObject("HookPoint").transform;anchor.position=root.transform.TransformPoint(new Vector3(-1.045f,-.004f,0));anchor.SetParent(jaw,true);visual.hookAnchor=anchor;
                var collision=root.AddComponent<CapsuleCollider>();collision.direction=0;collision.center=new Vector3(.1f,0,0);collision.radius=.29f;collision.height=2.5f;collision.isTrigger=true;
                if(!AssetDatabase.IsValidFolder("Assets/OceanAngler/Resources/Models"))AssetDatabase.CreateFolder("Assets/OceanAngler/Resources","Models");
                PrefabUtility.SaveAsPrefabAsset(root,"Assets/OceanAngler/Resources/Models/Bluefin.prefab");
                Debug.Log("BLUEFIN_UNITY_ASSET_OK clips="+clips.Length+" renderers="+renderers.Length+" bounds="+group.size);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            BuildTools.ApplyRenderQuality();AssetDatabase.SaveAssets();
        }
        static Texture2D Texture(string name)
        {var t=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+name+".png");if(t==null)throw new InvalidOperationException("Missing PBR map "+name);return t;}
        static Material CreateMaterial(string name,Color color,float smoothness)
        {
            string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.color=color;material.SetColor("_Color",color);material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",smoothness);return material;
        }
    }
}
