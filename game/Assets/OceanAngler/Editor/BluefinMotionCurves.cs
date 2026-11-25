using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace OceanAngler.Editor
{
    // Editable derivatives of our imported actions. Cyclic endpoint tangents
    // survive as native animation assets rather than being reduced by FBX import.
    public static class BluefinMotionCurves
    {
        public static AnimationClip Build(AnimationClip source,string semantic,string parent="Assets/OceanAngler/Art/Bluefin")
        {
            string folder=parent+"/Motions";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder(parent,"Motions");
            string path=folder+"/"+semantic+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
            EditorUtility.CopySerialized(source,clip);clip.name=semantic;
            bool looping=semantic!="release";var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=looping;AnimationUtility.SetAnimationClipSettings(clip,settings);
            if(looping)
            {
                var bindings=AnimationUtility.GetCurveBindings(clip);var curves=bindings.Select(b=>AnimationUtility.GetEditorCurve(clip,b)).ToArray();
                for(int i=0;i<bindings.Length;i++)
                {
                    if(bindings[i].type!=typeof(Transform))continue;var curve=curves[i];var keys=curve.keys;
                    if(keys.Length<3||keys.Max(k=>k.value)-keys.Min(k=>k.value)<1e-6f)continue;
                    int last=keys.Length-1;var first=keys[0];var end=keys[last];
                    if(Mathf.Abs(first.value-end.value)>1e-4f)throw new InvalidOperationException("Source action is not closed: "+semantic+" "+bindings[i].path+" "+bindings[i].propertyName);
                    float incoming=(end.value-keys[last-1].value)/(end.time-keys[last-1].time),outgoing=(keys[1].value-first.value)/(keys[1].time-first.time);
                    // Monotone periodic Hermite tangent: keep closed-jaw extrema
                    // flat and preserve travel through non-extremal loop points.
                    float tangent=incoming*outgoing>0?2*incoming*outgoing/(incoming+outgoing):0;
                    foreach(int index in new[]{0,last})
                    {
                        AnimationUtility.SetKeyBroken(curve,index,true);AnimationUtility.SetKeyLeftTangentMode(curve,index,AnimationUtility.TangentMode.Free);AnimationUtility.SetKeyRightTangentMode(curve,index,AnimationUtility.TangentMode.Free);
                        var key=curve.keys[index];key.inTangent=tangent;key.outTangent=tangent;key.weightedMode=WeightedMode.None;curve.MoveKey(index,key);
                    }
                }
                AnimationUtility.SetEditorCurves(clip,bindings,curves);
            }
            EditorUtility.SetDirty(clip);return clip;
        }
    }
}
