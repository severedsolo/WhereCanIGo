using System;
using System.Linq;
using UnityEngine;

namespace WhereCanIGo
{
    public class PlanetDeltaV
    {
        internal readonly string Name;
        internal readonly int EscapeDv;
        internal readonly string DisplayName = String.Empty;
        internal readonly int OrbitDv;
        internal readonly int SynchronousDv = -1;
        internal readonly int LandDv;
        internal readonly int ReturnFromFlybyDv;
        internal readonly int ReturnFromOrbitDv;
        internal readonly int ReturnFromLandingDv;
        internal readonly bool Setup;
        internal readonly bool RequireChutes;
        internal readonly CelestialBody RelatedBody;
        
        public PlanetDeltaV(ConfigNode setupNode)
        {
            Name = setupNode.GetValue("name");
            for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
            {
                CelestialBody cb = FlightGlobals.Bodies.ElementAt(i);
                if (cb.name != Name) continue;
                RelatedBody = cb;
                break;
            }
            if (RelatedBody == null)
            {
                Debug.Log("[WhereCanIGo]: Error setting up "+Name+" - No corresponding body found");
                return;
            }
            int.TryParse(setupNode.GetValue("flybyDV"), out EscapeDv);
            setupNode.TryGetValue("synchronousDV", ref SynchronousDv);
            setupNode.TryGetValue("displayName", ref DisplayName);
            int.TryParse(setupNode.GetValue("orbitDV"), out OrbitDv);
            int.TryParse(setupNode.GetValue("landDV"), out LandDv);
            int.TryParse(setupNode.GetValue("returnFromFlybyDV"), out ReturnFromFlybyDv);
            int.TryParse(setupNode.GetValue("returnFromOrbitDV"), out ReturnFromOrbitDv);
            int.TryParse(setupNode.GetValue("returnFromLandingDV"), out ReturnFromLandingDv);
            bool.TryParse(setupNode.GetValue("requireChutes"), out RequireChutes);
            Setup = true;
            Debug.Log("[WhereCanIGo]: Setup "+Name+" EscapeDV: "+EscapeDv+" OrbitDV: "+OrbitDv+ "LandDV: "+LandDv);
        }
    }
}