using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace OceanAngler
{
    public sealed class RuntimeSmoke : MonoBehaviour
    {
        [Serializable] sealed class Receipt { public string status="RUNNING";public List<string> checks=new List<string>();public List<string> failures=new List<string>();public int catches,points; }
        GameController game;string folder;Receipt receipt;bool finished;float began;
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--ocean-smoke");if(index<0||index+1>=args.Length)yield break;
            folder=args[index+1];Directory.CreateDirectory(folder);receipt=new Receipt();began=Time.realtimeSinceStartup;game=GetComponent<GameController>();Application.runInBackground=true;
            yield return new WaitForSecondsRealtime(1);
            if(game.Session.Paused){Click("继续探索");yield return null;yield return null;}
            CheckLayout("fishing");yield return Snapshot("01-fishing");
            int before=game.Progress.Data.catches.Count;
            var cast=Find("按住蓄力");Pointer(cast,true);yield return new WaitForSecondsRealtime(.4f);Pointer(cast,false);
            float timeout=Time.realtimeSinceStartup+18;
            while(game.Session.Phase!=FishingPhase.Bite&&Time.realtimeSinceStartup<timeout)yield return null;
            Require(game.Session.Phase==FishingPhase.Bite,"cast-to-bite");
            yield return null;yield return null;
            Click("提竿");yield return null;yield return null;
            Require(game.Session.Phase==FishingPhase.Fighting,"strike-to-fighting");
            bool held=false;Button reel=Find("按住收线");timeout=Time.realtimeSinceStartup+60;
            while(game.Session.Phase==FishingPhase.Fighting&&Time.realtimeSinceStartup<timeout)
            {
                bool desired=game.Session.Tension<.65f;if(desired!=held){held=desired;Pointer(reel,held);}yield return null;
            }
            if(held)Pointer(reel,false);yield return null;yield return null;
            Require(game.Session.Phase==FishingPhase.Landed,"controlled-catch");CheckLayout("landed");yield return Snapshot("02-catch");
            Click("记录并放流");yield return null;yield return null;
            Require(game.Progress.Data.catches.Count==before+1&&game.Progress.Data.totalPoints>0,"release-and-persist");
            Click("图鉴",true);yield return null;yield return null;CheckLayout("journal");yield return Snapshot("03-journal");
            Click("钓具",true);yield return null;yield return null;CheckLayout("gear");yield return Snapshot("04-gear");
            Click("任务",true);yield return null;yield return null;CheckLayout("tasks");yield return Snapshot("05-tasks");
            Click("设置",true);yield return null;yield return null;CheckLayout("settings");Click("左手操作");yield return null;yield return null;
            Require(game.Progress.Data.settings.leftHanded,"left-hand-setting");
            Click("出海",true);yield return null;yield return null;Require(game.Session.Paused&&!game.Reeling,"navigation-clears-held-input");CheckLayout("left-handed");yield return Snapshot("06-left-handed");
            receipt.catches=game.Progress.Data.catches.Count;receipt.points=game.Progress.Data.totalPoints;receipt.status=receipt.failures.Count==0?"PASS":"FAIL";finished=true;
            File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));Debug.Log("OCEAN_SMOKE_"+receipt.status);Application.Quit(receipt.failures.Count==0?0:1);
        }
        Button Find(string text,bool exact=false)
        {
            var button=FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.isActiveAndEnabled&&b.interactable&&(exact?b.GetComponentInChildren<Text>()?.text==text:b.GetComponentInChildren<Text>()?.text.StartsWith(text)==true));
            if(button==null)throw new InvalidOperationException("Missing button "+text);return button;
        }
        void Click(string text,bool exact=false)
        {
            var b=Find(text,exact);Pointer(b,true);Pointer(b,false);
            ExecuteEvents.Execute(b.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
        }
        void Pointer(Button b,bool down)
        {
            var point=RectTransformUtility.WorldToScreenPoint(null,b.GetComponent<RectTransform>().position);
            var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
            if(down)ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerDownHandler);
            else ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerUpHandler);
        }
        void Require(bool condition,string name){if(condition)receipt.checks.Add(name);else receipt.failures.Add(name);}
        void CheckLayout(string page)
        {
            foreach(var t in FindObjectsByType<Text>(FindObjectsSortMode.None))
                if(t.isActiveAndEnabled&&t.text.Length>0&&t.GetComponentInParent<ScrollRect>()==null)
                    if(t.preferredHeight>t.rectTransform.rect.height+1)receipt.failures.Add("clipped-text:"+page+":"+t.text.Replace("\n"," "));
        }
        IEnumerator Snapshot(string name)
        {
            yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),tex.EncodeToPNG());Destroy(tex);
        }
        void Update()
        {
            if(receipt!=null&&!finished&&Time.realtimeSinceStartup-began>100)
            {
                receipt.status="FAIL";receipt.failures.Add("Runtime smoke timed out");finished=true;
                File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));Application.Quit(1);
            }
        }
        void OnDestroy()
        {
            if(receipt!=null&&!finished){receipt.status="INTERRUPTED";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));}
        }
    }
}
