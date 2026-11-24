using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace OceanAngler
{
    public sealed partial class RuntimeSmoke
    {
        [Serializable] sealed class SlopeView { public string frame,habitat;public Vector3 camera;public Color fog,sun;public float sunIntensity,seaLevel;public bool above; }
        [Serializable] sealed class SlopeResult { public string status;public string[] failures;public List<SlopeView> views;public List<HudSample> hud;public int catchesBefore,catchesAfter,pointsBefore,pointsAfter;public bool otherProgressAndSettingsPreserved; }
        readonly List<SlopeView> slopeViews=new List<SlopeView>();
        public IEnumerator RunSlopeReview(string outputFolder,bool captureFrames,bool quitProcess=false)
        {
            folder=Path.GetFullPath(outputFolder);Directory.CreateDirectory(folder);if(File.Exists(Path.Combine(folder,"receipt.json")))throw new InvalidOperationException("Use a new review folder");
            game=GetComponent<GameController>();began=Time.realtimeSinceStartup;finished=false;journeyMode=gearReviewMode=true;journeyQuit=quitProcess;completeContent=false;slopeViews.Clear();hudSamples.Clear();
            receipt=new Receipt{captureEnabled=captureFrames,scope=captureFrames?"native slope habitat and minimal HUD review on a genuine isolated progress copy; not OS touch, art or physical-device acceptance":"headless slope navigation regression; no rendered acceptance"};
            var before=game.Progress.Data.Clone();var stack=new Stack<IEnumerator>();stack.Push(SlopeReview());
            while(stack.Count>0&&!finished)
            {
                object next=null;bool moved=false;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){receipt.failures.Add("slope-exception:"+failure.GetType().Name+":"+failure.Message);break;}
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
            var after=game.Progress.Data;
            bool preserved=before.habitatId==after.habitatId&&before.tutorialComplete==after.tutorialComplete&&before.observations.SequenceEqual(after.observations)&&before.rodId==after.rodId&&before.reelId==after.reelId&&before.lineId==after.lineId&&before.lureId==after.lureId&&JsonUtility.ToJson(before.settings)==JsonUtility.ToJson(after.settings);
            Require(preserved,"slope-review-preserves-other-progress-and-settings");
            if(!finished)FinishJourney();
            File.WriteAllText(Path.Combine(folder,"slope.json"),JsonUtility.ToJson(new SlopeResult{status=receipt.status,failures=receipt.failures.ToArray(),views=new List<SlopeView>(slopeViews),hud=new List<HudSample>(hudSamples),catchesBefore=before.catches.Count,catchesAfter=after.catches.Count,pointsBefore=before.totalPoints,pointsAfter=after.totalPoints,otherProgressAndSettingsPreserved=preserved},true));
        }
        IEnumerator SlopeFrame(string name)
        {
            var habitat=GetComponent<OceanWorld>().ActiveHabitat;var sun=FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional&&l.isActiveAndEnabled);
            slopeViews.Add(new SlopeView{frame=name,habitat=game.Progress.Data.habitatId,camera=Camera.main.transform.position,fog=RenderSettings.fogColor,sun=sun.color,sunIntensity=sun.intensity,seaLevel=habitat.SeaLevel,above=GetComponent<GameUI>().HabitatSurfaceView});
            yield return JourneyFrame(name);
        }
        IEnumerator SlopeReview()
        {
            var args=LaunchArguments.Get();int index=Array.IndexOf(args,"--ocean-save-root");JourneyRequire(index>=0&&index+1<args.Length,"slope-explicit-isolated-save-root");
            JourneyRequire(game.Content.habitats.All(h=>h.unlockPoints<=game.Progress.Data.totalPoints)&&!game.Progress.RequiresRecovery&&game.Session.Phase==FishingPhase.Ready,"slope-valid-unlocked-genuine-progress-copy");
            string originalHabitat=game.Progress.Data.habitatId,save=Path.Combine(Path.GetFullPath(args[index+1]),"ocean-progress.json");var before=game.Progress.Data.Clone();
            yield return null;yield return null;
            // Obtain the undimmed scene baseline by leaving any deep habitat.
            yield return JourneyClick("海域与装备",true);yield return null;yield return null;
            yield return JourneyClick(game.Content.Habitat("bay").name,true);yield return null;yield return null;
            var sun=FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional&&l.isActiveAndEnabled);float intensity=sun.intensity;Color color=sun.color;
            foreach(string id in new[]{"slope","channel","bay","slope"})
            {
                yield return JourneyClick(game.Content.Habitat(id).name,true);yield return null;yield return null;
                var habitat=GetComponent<OceanWorld>().ActiveHabitat;JourneyRequire(habitat!=null&&game.Progress.Data.habitatId==id,"slope-authored-habitat-"+id);
                JourneyRequire(Mathf.Abs(sun.intensity-intensity*(id=="slope"?.13f:1))<.0001f,"slope-light-roundtrip-"+id);
                if(id!="slope")JourneyRequire(sun.color==color,"slope-scene-sun-color-restored-"+id);
                yield return JourneyClick(id=="bay"?"看看海湾":"看看海域",true);yield return null;yield return null;
                var ui=GetComponent<GameUI>();if(!ui.HabitatSurfaceView){yield return JourneyClick("回到水面",true);yield return null;yield return null;}
                JourneyRequire(sun.intensity==intensity&&sun.color==color,"slope-surface-original-sun-"+id);
                string prefix=slopeViews.Count.ToString("D2")+"-"+id;
                yield return SlopeFrame(prefix+"-surface");yield return JourneyClick("换个角度",true);yield return null;yield return null;yield return SlopeFrame(prefix+"-boat");
                yield return JourneyClick("潜入水下",true);yield return null;yield return null;yield return SlopeFrame(prefix+"-underwater");
                yield return JourneyClick("换个角度",true);yield return null;yield return null;yield return SlopeFrame(prefix+"-alternate");
                yield return JourneyClick("返回钓鱼",true);yield return null;yield return null;yield return JourneyClick("海域与装备",true);yield return null;yield return null;
            }
            yield return JourneyClick("开始钓鱼",true);yield return null;yield return null;CheckImmersiveHud();yield return JourneyFrame("slope-ready");
            var cast=Find("按住蓄力");Pointer(cast,true);yield return new WaitForSecondsRealtime(.3f);Pointer(cast,false);
            float deadline=Time.realtimeSinceStartup+20;while(game.Session.Phase!=FishingPhase.Bite&&Time.realtimeSinceStartup<deadline)yield return null;
            JourneyRequire(game.Session.Phase==FishingPhase.Bite,"slope-normal-bite");yield return null;yield return null;CheckImmersiveHud();yield return JourneyClick("提竿",true);yield return null;yield return null;CheckImmersiveHud();yield return JourneyFrame("slope-fighting");
            var reel=Find("按住收线");bool held=false;deadline=Time.realtimeSinceStartup+90;
            while(game.Session.Phase==FishingPhase.Fighting&&Time.realtimeSinceStartup<deadline){bool desired=game.Session.Tension<.65f;if(desired!=held){held=desired;Pointer(reel,held);}yield return null;}
            if(held)Pointer(reel,false);yield return null;yield return null;JourneyRequire(game.Session.Phase==FishingPhase.Landed,"slope-real-catch-landed");yield return JourneyFrame("slope-landed");
            yield return JourneyClick("记录并放流",true);yield return null;yield return null;yield return new WaitForSecondsRealtime(.65f);yield return JourneyFrame("slope-release");
            JourneyRequire(game.Progress.Data.catches.Count==before.catches.Count+1&&game.Progress.Data.catches.Last().habitatId=="slope","slope-one-new-native-catch");
            yield return JourneyClick("图鉴",true);yield return null;yield return null;yield return JourneyClick("保护观察");yield return null;yield return null;yield return JourneyClick("观察鲸鲨",true);yield return null;yield return null;yield return JourneyClick("安静观察",true);
            deadline=Time.realtimeSinceStartup+8;while(!GetComponent<OceanWorld>().Observations.CanRecord&&Time.realtimeSinceStartup<deadline)yield return null;
            JourneyRequire(GetComponent<OceanWorld>().Observations.CanRecord,"slope-whale-shark-visit-played");yield return JourneyFrame("slope-whale-shark");
            // Watching this already-known species creates no reward or record.
            yield return JourneyClick("出海",true);yield return null;yield return null;
            yield return JourneyClick("海域与装备",true);yield return null;yield return null;yield return JourneyClick(game.Content.Habitat(originalHabitat).name,true);yield return null;yield return null;
            var loaded=new SaveStore(save,game.Content).Load();JourneyRequire(!loaded.requiresRecovery&&loaded.data.habitatId==originalHabitat&&loaded.data.catches.Count==before.catches.Count+1&&loaded.data.catches.Take(before.catches.Count).Select(c=>JsonUtility.ToJson(c)).SequenceEqual(before.catches.Select(c=>JsonUtility.ToJson(c))),"slope-existing-record-prefix-and-restored-habitat-on-disk");
            receipt.pixelsPerPoint=DisplayMetrics.PixelsPerPoint;receipt.safeWidthPoints=DisplayMetrics.SafeArea.width/DisplayMetrics.PixelsPerPoint;receipt.safeHeightPoints=DisplayMetrics.SafeArea.height/DisplayMetrics.PixelsPerPoint;
        }
    }
}
