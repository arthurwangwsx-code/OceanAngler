using UnityEngine;
namespace OceanAngler
{
    public sealed class FeedbackPlayer : MonoBehaviour
    {
        GameController game;AudioSource source,ambience,music;AudioClip bite,landed,escape;FishingPhase previous;
        void Start()
        {
            game=GetComponent<GameController>();source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=0;source.priority=64;
            bite=Tone("Bite",660,880);landed=Tone("Discovery",523,784);escape=Tone("Retry",392,330);
            ambience=Loop("OceanAmbience","OceanBed",192);music=Loop("ExploreMusic","ExploreTheme",200);
        }
        AudioSource Loop(string name,string resource,int priority)
        {
            var child=new GameObject(name);child.transform.SetParent(transform,false);var player=child.AddComponent<AudioSource>();
            player.playOnAwake=false;player.spatialBlend=0;player.volume=0;player.loop=true;player.priority=priority;
            player.clip=Resources.Load<AudioClip>("Audio/"+resource);
            if(player.clip==null)throw new System.InvalidOperationException("Missing authored audio "+resource);
            return player;
        }
        AudioClip Tone(string name,float first,float second)
        {
            const int rate=22050;var data=new float[rate/2];
            float phase=0;
            for(int i=0;i<data.Length;i++){float time=i/(float)rate;float frequency=Mathf.Lerp(first,second,Mathf.SmoothStep(0,1,(time-.18f)/.04f));float envelope=Mathf.Clamp01(time/.02f)*Mathf.Clamp01((.5f-time)/.08f);phase+=frequency*Mathf.PI*2/rate;data[i]=Mathf.Sin(phase)*envelope*.45f;}
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        void Update()
        {
            if(game.Progress.RequiresRecovery||game.Session.Paused){StopAll();previous=game.Session.Phase;return;}
            var settings=game.Progress.Data.settings;
            if(settings.sound)source.volume=.18f;else Stop(source);
            MixLoop(ambience,settings.sound,.08f);MixLoop(music,settings.music,.08f);
            if(previous==game.Session.Phase)return;previous=game.Session.Phase;
            var clip=previous==FishingPhase.Bite?bite:previous==FishingPhase.Landed?landed:previous==FishingPhase.Escaped?escape:null;
            if(clip!=null&&settings.sound)source.PlayOneShot(clip);
#if UNITY_IOS && !UNITY_EDITOR
            if(previous==FishingPhase.Bite&&settings.haptics)Handheld.Vibrate();
#endif
        }
        static void MixLoop(AudioSource player,bool enabled,float target)
        {
            if(!enabled){Stop(player);return;}
            player.volume=Mathf.MoveTowards(player.volume,target,Time.unscaledDeltaTime*.16f);
            if(!player.isPlaying)player.Play();
        }
        static void Stop(AudioSource player){if(player==null)return;player.volume=0;player.Stop();}
        void StopAll(){Stop(source);Stop(ambience);Stop(music);}
        void OnDisable(){StopAll();}
        void OnApplicationFocus(bool focus){if(!focus)StopAll();}
        void OnApplicationPause(bool paused){if(paused)StopAll();}
        void OnDestroy(){if(bite!=null)Destroy(bite);if(landed!=null)Destroy(landed);if(escape!=null)Destroy(escape);}
    }
}
