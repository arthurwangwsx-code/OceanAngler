using UnityEngine;
namespace OceanAngler
{
    public sealed class FeedbackPlayer : MonoBehaviour
    {
        GameController game;AudioSource source;AudioClip bite,landed,escape;FishingPhase previous;
        void Start()
        {
            game=GetComponent<GameController>();source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=.18f;
            bite=Tone("Bite",660,880);landed=Tone("Discovery",523,784);escape=Tone("Retry",392,330);
        }
        AudioClip Tone(string name,float first,float second)
        {
            const int rate=22050;var data=new float[rate/2];
            for(int i=0;i<data.Length;i++){float time=i/(float)rate;float frequency=time<.2f?first:second;float envelope=Mathf.Clamp01(time/.02f)*Mathf.Clamp01((.5f-time)/.08f);data[i]=Mathf.Sin(time*frequency*Mathf.PI*2)*envelope*.45f;}
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        void Update()
        {
            if(game.Progress.RequiresRecovery)return;
            if(game.Session.Paused){source.Stop();return;}
            if(previous==game.Session.Phase)return;previous=game.Session.Phase;
            var clip=previous==FishingPhase.Bite?bite:previous==FishingPhase.Landed?landed:previous==FishingPhase.Escaped?escape:null;
            if(clip!=null&&game.Progress.Data.settings.sound)source.PlayOneShot(clip);
#if UNITY_IOS && !UNITY_EDITOR
            if(previous==FishingPhase.Bite&&game.Progress.Data.settings.haptics)Handheld.Vibrate();
#endif
        }
        void OnDestroy(){if(bite!=null)Destroy(bite);if(landed!=null)Destroy(landed);if(escape!=null)Destroy(escape);}
    }
}
