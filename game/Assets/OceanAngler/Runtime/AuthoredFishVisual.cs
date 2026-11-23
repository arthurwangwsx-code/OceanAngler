using System.Collections.Generic;
using UnityEngine;
namespace OceanAngler
{
    // 导入资产和其余物种共用玩法接口；美术候选不改变积分或捕获规则。
    public sealed class AuthoredFishVisual : FishVisual
    {
        public Transform hookAnchor;
        public override Vector3 HookPosition=>hookAnchor==null?base.HookPosition:hookAnchor.position;
        Animator animator;
        string current;
        sealed class Binding
        {public SkinnedMeshRenderer renderer;public int index;public Color color;public float smoothness;public readonly MaterialPropertyBlock block=new MaterialPropertyBlock();}
        readonly List<Binding> bindings=new List<Binding>();
        bool hidden;
        void Awake(){InitializePresentation();}
        public void InitializePresentation()
        {
            if(bindings.Count!=0)return;
            animator=GetComponentInChildren<Animator>();
            foreach(var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var shared=renderer.sharedMaterials;
                for(int i=0;i<shared.Length;i++)
                {
                    var binding=new Binding{renderer=renderer,index=i,color=shared[i].color,smoothness=shared[i].GetFloat("_Smoothness")};renderer.GetPropertyBlock(binding.block,i);bindings.Add(binding);Apply(binding,false);
                }
            }
        }
        void Update()
        {
            if(animator==null)return;
            animator.speed=paused||reducedMotion?0:Mathf.Max(0,speed);
            string desired=string.IsNullOrEmpty(semantic)?"swim":semantic;
            if(current!=desired&&animator.HasState(0,Animator.StringToHash(desired)))
            {animator.CrossFadeInFixedTime(desired,.18f);current=desired;}
        }
        public override void SetSilhouette(bool value)
        {
            if(hidden==value)return;hidden=value;
            foreach(var binding in bindings)Apply(binding,hidden);
        }
        static void Apply(Binding binding,bool silhouette)
        {
            binding.block.SetColor("_BaseColor",silhouette?new Color(.008f,.022f,.028f):binding.color);binding.block.SetFloat("_Smoothness",silhouette?0:binding.smoothness);binding.renderer.SetPropertyBlock(binding.block,binding.index);
        }
        public void ReleasePresentation(){foreach(var binding in bindings)if(binding.renderer!=null)binding.renderer.SetPropertyBlock(null,binding.index);bindings.Clear();hidden=false;}
        void OnDestroy(){ReleasePresentation();}
    }
}
