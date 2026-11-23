using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace OceanAngler
{
    public sealed partial class GameUI
    {
        string observationId;
        Text observationStatus,observationPlayback;Button observationRecord;
        public bool InspectingObservation=>page=="观察生物";
        public bool ObservationJournal=>page=="保护观察"||InspectingObservation;
        public string ObservingSpecies=>InspectingObservation?observationId:null;
        void ObservationEntry(RectTransform parent,float top)
        {
            Button(parent,"保护观察  "+game.Progress.Data.observations.Count+" / 3",22,top,-44,44,()=>Navigate("保护观察"),Navy).GetComponentInChildren<Text>().fontSize=18;
        }
        void BuildObservations()
        {
            var panel=PagePanel("ObservationJournal",new Vector2(.02f,.12f),new Vector2(.98f,.81f),560);
            Label(panel,"保护观察  "+game.Progress.Data.observations.Count+" / 3",Compact?22:28,Color.white,22,12,-44,40);
            Label(panel,"安静观察，不投饵、不挂钩。阅读图鉴不会自动记为发现。",18,Muted,22,54,-44,48);
            for(int i=0;i<game.Content.observations.Length;i++)
            {
                var species=game.Content.observations[i];float y=110+i*140;
                bool known=game.Progress.Data.observations.Contains(species.id),unlocked=game.Progress.Data.totalPoints>=game.Content.Habitat(species.habitatId).unlockPoints;
                Label(panel,species.name+(known?" · 已观察":" · 未观察"),22,known?Gold:Color.white,22,y,-44,32);
                Label(panel,species.scientificName+"\n"+game.Content.Habitat(species.habitatId).name+" · "+species.note,17,Muted,22,y+34,-44,58);
                bool travel=species.habitatId!=game.Progress.Data.habitatId;
                string label=!unlocked?"探索积分 "+game.Content.Habitat(species.habitatId).unlockPoints+" 后前往":travel&&game.Session.Phase!=FishingPhase.Ready?"返回准备后前往":"观察"+species.name;
                var button=Button(panel,label,22,y+94,-44,44,()=>
                {
                    if(travel)game.SelectHabitat(species.habitatId);
                    if(game.Progress.Data.habitatId==species.habitatId){observationId=species.id;Navigate("观察生物");}
                },Navy);button.interactable=unlocked&&(!travel||game.Session.Phase==FishingPhase.Ready);button.GetComponentInChildren<Text>().fontSize=18;
            }
        }
        void BuildObservationVisit()
        {
            var species=game.Content.Observation(observationId);float width=Mathf.Min(340,Width*.44f);
            var panel=Box(pageRoot,"ObservationDetails",Panel,new Vector2(1,0),Vector2.one,new Vector2(-width-12,36),new Vector2(-12,Compact?-102:-156));
            var viewport=Box(panel,"ObservationViewport",Color.clear,Vector2.zero,Vector2.one,new Vector2(0,148),Vector2.zero);viewport.gameObject.AddComponent<RectMask2D>();
            var content=new GameObject("ObservationContent",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(viewport,false);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;
            var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(14,14,10,10);layout.spacing=10;layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandWidth=layout.childForceExpandHeight=false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            Add("ObservationName",species.name,24,Color.white);Add("ObservationScientific",species.scientificName,18,Muted);Add("ObservationRecognition",species.recognition,18,Color.white);Add("ObservationHabitat",species.naturalHabitat,18,Muted);Add("ObservationCare",species.note+" 游戏海域是想象的探索安排。",17,Muted);
            // Bottom anchored controls stay reachable while the facts scroll.
            var controls=At(panel,"ObservationControls",Panel,0,0,0,148,true);
            var playback=Button(controls,"安静观察",12,4,-24,44,()=>game.GetComponent<OceanWorld>().Observations.Toggle(),Navy);observationPlayback=playback.GetComponentInChildren<Text>();observationPlayback.fontSize=18;
            observationStatus=Label(controls,"",17,Muted,12,50,-24,42);
            observationRecord=Button(controls,"记入观察图鉴",12,98,-24,44,game.RecordObservation,Teal);observationRecord.GetComponentInChildren<Text>().fontSize=18;
            Button(pageRoot,"返回观察图鉴",12,102,180,44,()=>Navigate("保护观察"),Navy).GetComponentInChildren<Text>().fontSize=18;
            void Add(string name,string text,int size,Color color){var label=Label(content,text,size,color,0,0,0,80);label.name=name;label.gameObject.AddComponent<LayoutElement>().minHeight=size*1.5f;}
        }
        void UpdateObservationUI()
        {
            if(!InspectingObservation||observationRecord==null)return;
            var encounter=game.GetComponent<OceanWorld>().Observations;if(encounter==null)return;
            bool known=game.Progress.Data.observations.Contains(observationId);
            observationRecord.interactable=!known&&encounter.CanRecord;
            observationRecord.GetComponentInChildren<Text>().text=known?"已记入观察图鉴":"记入观察图鉴";
            observationPlayback.text=encounter.Playing?"暂停观察":"安静观察";
            observationStatus.text=known?"已经认识这位海洋朋友。":encounter.CanRecord?"看清它了，可以记入图鉴。":encounter.Playing?"安静看看它的模样……":"点安静观察，再记下新发现。";
        }
    }
}
