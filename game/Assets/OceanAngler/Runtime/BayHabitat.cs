using UnityEngine;
namespace OceanAngler
{
    public sealed class BayHabitat : MarineHabitat
    {
        public BayFishSchool school;
        protected override void PresentAmbient(bool paused,bool reduced,bool visible)
        {if(school!=null)school.Present(Time.deltaTime,paused,reduced,visible);}
    }
}
