using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// ReSharper disable MemberCanBeMadeStatic.Global

namespace WhereCanIGo
{
    public class Utilities
    {
        internal readonly List<PlanetDeltaV> Planets = new List<PlanetDeltaV>();
        private readonly PopupDialog _uiDialog;
        private readonly Rect _geometry = new Rect(0.5f, 0.5f, 800, 30);
        internal readonly string SystemNotes;

        public  Utilities()
        {
            ConfigNode deltaVNode = GameDatabase.Instance.GetConfigNode("WhereCanIGo/WHERE_CAN_I_GO");
            if (deltaVNode == null)
            {
                _uiDialog = GenerateErrorDialog();
                Debug.Log("[WhereCanIGo]: deltaVNode is null");
                return;
            }
            SystemNotes = deltaVNode.GetValue("notes");
            ConfigNode[] bodies = deltaVNode.GetNodes("BODY");
            for (int i = 0; i < bodies.Length; i++)
            {
                ConfigNode cn = bodies.ElementAt(i);
                PlanetDeltaV planetToSetup = new PlanetDeltaV(cn);
                if (planetToSetup.Setup) Planets.Add(planetToSetup);
            }

            if (Planets.Count != 0) return;
            Debug.Log("[WhereCanIGo]: No planets were setup");
            _uiDialog = GenerateErrorDialog();
        }
        
        private PopupDialog GenerateErrorDialog()
        {
            List<DialogGUIBase> dialog = new List<DialogGUIBase>
            {
                new DialogGUILabel("WARNING: Where Can I Go couldn't find settings for your solar system."),
                new DialogGUIButton("OK", () => CloseDialog(_uiDialog), false)
            };
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new MultiOptionDialog("ErrorDialog", "", "Where Can I Go", UISkinManager.defaultSkin, _geometry,
                    dialog.ToArray()), false, UISkinManager.defaultSkin);
        }
        
        internal void CloseDialog(PopupDialog dialogToClose)
        {
            dialogToClose.Dismiss();
        }
        internal UIStyle GenerateStyle(int deltaV, bool largePrint)
        {
            int fontsize = 12;
            if(largePrint) fontsize = 24;
            FontStyle fontstyle = FontStyle.Normal;
            if (largePrint) fontstyle = FontStyle.Bold;
            UIStyle style = new UIStyle
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = fontstyle,
                normal = new UIStyleState(),
                fontSize = fontsize
            };
            style.normal.textColor = GetTextColor(deltaV);
            return style;
        }

        internal bool SituationValid(CelestialBody planetRelatedBody, string situation)
        {
            switch (situation)
            {
                case "Flyby: " when planetRelatedBody == FlightGlobals.GetHomeBody():
                case "Landing: " when !planetRelatedBody.hasSolidSurface:
                    return false;
                default:
                    return true;
            }
        }

        internal string VesselStatus(int deltaVRequired, string situation, PlanetDeltaV planet)
        {
            double vesselDeltaV;
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (EditorLogic.fetch.ship == null || EditorLogic.fetch.ship.vesselDeltaV == null) return "N/A";
                vesselDeltaV = EditorLogic.fetch.ship.vesselDeltaV.TotalDeltaVVac;
            }
            else
            {
                if (FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.VesselDeltaV == null) return "N/A";
                vesselDeltaV = FlightGlobals.ActiveVessel.VesselDeltaV.TotalDeltaVVac;
            }
            if (deltaVRequired < 0) return "N/A";
            if (deltaVRequired > vesselDeltaV) return "NO";
            if (vesselDeltaV - deltaVRequired < deltaVRequired * 0.05) return "MARGINAL";
            if (situation == "Landing: " && planet != null && planet.RequireChutes) return "YES*";
            return "YES";
        }

        private Color GetTextColor(int deltaV)
        {
            switch (VesselStatus(deltaV, "", null))
            {
                case "NO":
                    return Color.red;
                case "MARGINAL":
                    return Color.yellow;
                case "YES":
                    return Color.green;
                default:
                    return Color.green;
            }
        }

        internal UIStyle CreateNoteStyle()
        {
            int fontsize = 12;
            UIStyle style = new UIStyle
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Normal,
                normal = new UIStyleState(),
                fontSize = fontsize
            };
            style.normal.textColor = Color.cyan;
            return style;
        }
    }
}