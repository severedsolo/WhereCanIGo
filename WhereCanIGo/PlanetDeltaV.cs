using System.Linq;
using LibNoise;
using UnityEngine;
using Math = LibNoise.Math;

namespace WhereCanIGo
{
    public class PlanetDeltaV
    {
        internal readonly string Name;
        internal int EscapeDv;
        private readonly string _displayName = "";
        internal int OrbitDv;
        internal int SynchronousDv = -1;
        internal int LandDv;
        internal int ReturnFromFlybyDv;
        internal int ReturnFromOrbitDv;
        internal int ReturnFromLandingDv;
        internal bool Setup;
        internal bool RequireChutes;
        internal bool IsHomeWorld;
        internal readonly CelestialBody RelatedBody;

        public PlanetDeltaV(ConfigNode setupNode, double rescaleFactor)
        {

            Name = setupNode.GetValue("name");
            for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
            {
                CelestialBody cb = FlightGlobals.Bodies.ElementAt(i);
                if (cb.name != Name) continue;
                RelatedBody = cb;
                if (RelatedBody == FlightGlobals.GetHomeBody()) IsHomeWorld = true;
                break;
            }
            if (RelatedBody == null)
            {
                Debug.Log("[WhereCanIGo]: Error setting up "+Name+" - No corresponding body found");
                return;
            }
            int.TryParse(setupNode.GetValue("flybyDV"), out EscapeDv);
            if(!setupNode.TryGetValue("synchronousDV", ref SynchronousDv)) SynchronousDv = -1;
            if (!setupNode.TryGetValue("displayName", ref _displayName)) _displayName = "";
            int.TryParse(setupNode.GetValue("orbitDV"), out OrbitDv);
            int.TryParse(setupNode.GetValue("landDV"), out LandDv);
            int.TryParse(setupNode.GetValue("returnFromFlybyDV"), out ReturnFromFlybyDv);
            int.TryParse(setupNode.GetValue("returnFromOrbitDV"), out ReturnFromOrbitDv);
            int.TryParse(setupNode.GetValue("returnFromLandingDV"), out ReturnFromLandingDv);
            bool.TryParse(setupNode.GetValue("requireChutes"), out RequireChutes);
            RescaleSystem(rescaleFactor);
            Setup = true;
            Debug.Log("[WhereCanIGo]: Setup "+Name+" EscapeDV: "+EscapeDv+" OrbitDV: "+OrbitDv+ "LandDV: "+LandDv);
        }

        private void RescaleSystem(double rescaleFactor)
        {
            EscapeDv = (int) (EscapeDv* rescaleFactor);
            SynchronousDv = (int) (SynchronousDv * rescaleFactor);
            OrbitDv = (int)(OrbitDv * rescaleFactor);
            LandDv = (int)(LandDv * rescaleFactor);
            ReturnFromFlybyDv = (int)(ReturnFromFlybyDv * rescaleFactor);
            ReturnFromOrbitDv = (int)(ReturnFromOrbitDv * rescaleFactor);
            ReturnFromLandingDv = (int)(ReturnFromLandingDv * rescaleFactor);
        }

        internal string GetName()
        {
            return _displayName != "" ? _displayName : Name;
        }
    }
}