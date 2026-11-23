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
        readonly List<Material> materials=new List<Material>();
        readonly List<Color> colors=new List<Color>();
        readonly List<float> smoothness=new List<float>();
        bool hidden;
        void Awake()
        {
            animator=GetComponentInChildren<Animator>();
            foreach(var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                foreach(var material in renderer.materials)
                { materials.Add(material);colors.Add(material.color);smoothness.Add(material.GetFloat("_Smoothness")); }
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
            for(int i=0;i<materials.Count;i++)
            {materials[i].color=hidden?new Color(.008f,.022f,.028f):colors[i];materials[i].SetFloat("_Smoothness",hidden?0:smoothness[i]);}
        }
        void OnDestroy(){foreach(var material in materials)if(material!=null)Destroy(material);}
    }
}
