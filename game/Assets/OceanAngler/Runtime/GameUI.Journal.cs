using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace OceanAngler
{
    public sealed partial class GameUI
    {
        void BuildFishDetails(RectTransform panel,FishDefinition target)
        {
            var viewport=Box(panel,"SpeciesViewport",Color.clear,Vector2.zero,Vector2.one,new Vector2(0,30),Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content=new GameObject("SpeciesContent",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(viewport,false);
            content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
            var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(14,14,12,12);layout.spacing=10;
            layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandWidth=layout.childForceExpandHeight=false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=panel.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            var records=game.Progress.Data.catches.Where(c=>c.fishId==target.id).ToArray();
            Add("SpeciesName",target.name,Compact?23:28,Color.white);
            Add("ScientificName",target.scientificName,19,Muted);
            Add("CatchSummary",records.Length==0?"尚未记录这位朋友。\n出海发现后，模型会亮起来。":"我的记录 · "+records.Length+" 次\n最长 "+records.Max(c=>c.length).ToString("F1")+" 厘米\n最重 "+records.Max(c=>c.weight).ToString("F2")+" 千克",18,Gold);
            Add("Recognition","认识它\n"+target.note,18,Color.white);
            Add("NaturalHabitat","自然栖息环境\n"+target.naturalHabitat,18,Muted);
            Add("ExplorationNote","游戏海域是想象的探索场景，不代表这些鱼在现实中共同生活。",17,Muted);
            var hint=At(panel,"SpeciesHint",Color.clear,0,0,0,30,true);
            Label(hint,"上下滑动查看详情",17,Muted,14,2,-28,26);
            void Add(string name,string text,int size,Color color)
            {
                var label=Label(content,text,size,color,0,0,0,100);label.name=name;
                label.gameObject.AddComponent<LayoutElement>().minHeight=size*1.5f;
            }
        }
        void BuildJournalControls(float left,float top,float available)
        {
            string[] names={"旋转","放大","缩小",ModelPaused?"游动":"静止","重置"};
            Action[] actions={()=>ModelYaw+=30,()=>ModelZoom=Mathf.Min(1.5f,ModelZoom+.15f),()=>ModelZoom=Mathf.Max(.7f,ModelZoom-.15f),()=>{ModelPaused=!ModelPaused;dirty=true;},()=>{ModelYaw=0;ModelZoom=1;ModelPaused=false;dirty=true;}};
            float width=(available-24)/5;
            for(int i=0;i<names.Length;i++)Button(pageRoot,names[i],left+i*(width+6),top,width,44,actions[i],Navy).GetComponentInChildren<Text>().fontSize=17;
        }
    }
}
