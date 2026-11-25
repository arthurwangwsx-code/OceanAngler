using UnityEngine;
namespace OceanAngler
{
    public sealed partial class OceanWorld
    {
        bool flatfishViewActive;
        Vector3 flatfishEye,flatfishLook;
        void PresentFlatfishCamera(bool reduced)
        {
            bool holdingRelease=flatfishViewActive&&awaitingNextFish&&game.Session.Phase==FishingPhase.Ready;
            bool target=game.Session.Catch!=null&&game.Session.Catch.fishId==fishId;
            bool inspecting=ui.FocusedFish!=null||ui.InspectingGear||ui.InspectingHabitat||ui.InspectingObservation;
            if(FishingSurfaceView||inspecting||game.Content.Fish(fishId).shape!="flat"||!target&&!holdingRelease)
            {flatfishViewActive=false;return;}
            if(!holdingRelease&&(!game.Session.Paused||!flatfishViewActive))
            {
                var look=fishRoot.position;
                // An elevated close view reveals the eyed side while the fish
                // keeps its natural horizontal pose. Stay below the waterline.
                var eye=look+new Vector3(0,2.8f,-3.8f);eye.y=Mathf.Min(eye.y,activeHabitat.SeaLevel-.6f);
                float blend=!flatfishViewActive||reduced?1:1-Mathf.Exp(-4*Mathf.Clamp(Time.deltaTime,0,.1f));
                flatfishEye=Vector3.Lerp(flatfishEye,eye,blend);flatfishLook=Vector3.Lerp(flatfishLook,look,blend);
            }
            flatfishViewActive=true;
            // Keep the final view during release, so the fish swims away from
            // the viewer rather than being followed out into the habitat.
            view.transform.SetPositionAndRotation(flatfishEye,Quaternion.LookRotation(flatfishLook-flatfishEye,Vector3.up));
        }
    }
}
