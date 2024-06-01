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
        internal readonly string Warnings;

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
            Warnings = deltaVNode.GetValue("warning");
            double rescaleFactor = 1;
            if (!deltaVNode.TryGetValue("rescaleFactor", ref rescaleFactor)) rescaleFactor = 1;
            rescaleFactor = System.Math.Sqrt(rescaleFactor);
            ConfigNode[] bodies = deltaVNode.GetNodes("BODY");
            for (int i = 0; i < bodies.Length; i++)
            {
                ConfigNode cn = bodies.ElementAt(i);
                PlanetDeltaV planetToSetup = new PlanetDeltaV(cn, rescaleFactor);
                if (planetToSetup.Setup) Planets.Add(planetToSetup);
            }

            if (Planets.Count != 0) return;
            Debug.Log("[WhereCanIGo]: No planets were setup");
            _uiDialog = GenerateErrorDialog();
        }
        
        public PlanetDeltaV ConvertBodyToPlanetDeltaV(CelestialBody cb)
        {
            for (int i = 0; i < Planets.Count; i++)
            {
                PlanetDeltaV p = Planets.ElementAt(i);
                if (p.Name == cb.bodyName) return p;
            }
            return null;
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
            ConfigNode modNode = GameDatabase.Instance.GetConfigNode("WhereCanIGo/WHERE_CAN_I_GO");
            switch (VesselStatus(deltaV, "", null))
            {
                case "NO":
                    return ParseColor(modNode.GetValue("colorIfNo"));
                case "MARGINAL":
                    return ParseColor(modNode.GetValue("colorIfMarginal"));
                case "YES":
                    return ParseColor(modNode.GetValue("colorIfYes"));
                default:
                    return ParseColor(modNode.GetValue("colorIfYes"));
            }
        }

        private Color ParseColor(string color)
        {
            color = color.ToLower();
            switch (color)
            {
                case "black":
                    return Color.black;
                case "blue":
                    return Color.blue;
                case "cyan":
                    return Color.cyan;
                case "gray":
                    return Color.gray;
                case "green":
                    return Color.green;
                case "magenta":
                    return Color.magenta;
                case "red":
                    return Color.red;
                case "white":
                    return Color.white;
                case "yellow":
                    return Color.yellow;
                default:
                    return Color.black;
            }
        }


        internal UIStyle CreateNoteStyle()
        {
            const int fontsize = 12;
            UIStyle style = new UIStyle
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                normal = new UIStyleState(),
                fontSize = fontsize
            };
            style.normal.textColor = Color.cyan;
            return style;
        }
    }
}