using System;
using UnityEngine;
namespace OceanAngler
{
    public static class SurfaceMaterial
    {
        public static Material Create()
        {
            var template=Resources.Load<Material>("Surface");
            if(template==null)throw new InvalidOperationException("Missing Surface material resource");
            return new Material(template);
        }
    }
}
