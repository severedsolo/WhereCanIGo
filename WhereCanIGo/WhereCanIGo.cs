using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using System;
using UnityEngine;
// ReSharper disable SuggestVarOrType_Elsewhere

namespace WhereCanIGo
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class WhereCanIGo : MonoBehaviour
    {
        private ApplicationLauncherButton _toolbarButton;
        private PopupDialog _uiDialog;
        private readonly Rect _geometry = new Rect(0.5f, 0.5f, 800, 30);
        private readonly List<PlanetDeltaV> _planets = new List<PlanetDeltaV>();
        private bool _returnTrip;

        private void Awake()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(AddToolbarButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(RemoveToolbarButton);
        }
        

        private void Start()
        {
            ConfigNode deltaVNode = GameDatabase.Instance.GetConfigNode("WhereCanIGo/WHERE_CAN_I_GO");
            if (deltaVNode == null)
            {
                _uiDialog = GenerateErrorDialog();
                Debug.Log("[WhereCanIGo]: deltaVNode is null");
                return;
            }

            ConfigNode[] bodies = deltaVNode.GetNodes("BODY");
            for (int i = 0; i < bodies.Length; i++)
            {
                ConfigNode cn = bodies.ElementAt(i);
                PlanetDeltaV planetToSetup = new PlanetDeltaV(cn);
                if (planetToSetup.Setup) _planets.Add(planetToSetup);
            }

            if (_planets.Count != 0) return;
            Debug.Log("[WhereCanIGo]: No planets were setup");
            _uiDialog = GenerateErrorDialog();
        }

        private PopupDialog GenerateErrorDialog()
        {
            List<DialogGUIBase> dialog = new List<DialogGUIBase>
            {
                new DialogGUILabel("WARNING: Where Can I Go couldn't find settings for your solar system."),
                new DialogGUIButton("OK", CloseDialog, false)
            };
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new MultiOptionDialog("ErrorDialog", "", "Where Can I Go", UISkinManager.defaultSkin, _geometry,
                    dialog.ToArray()), false, UISkinManager.defaultSkin);
        }

        private void CloseDialog()
        {
            _uiDialog.Dismiss();
        }

        private void AddToolbarButton()
        {
            if (_toolbarButton == null)
            {
                _toolbarButton = ApplicationLauncher.Instance.AddModApplication(SpawnDialog, SpawnDialog,
                    null, null, null, null, ApplicationLauncher.AppScenes.VAB,
                    GameDatabase.Instance.GetTexture("WhereCanIGo/Icon", false));
            }
        }

        private void RemoveToolbarButton(GameScenes whatever)
        {
            if(_toolbarButton != null) ApplicationLauncher.Instance.RemoveModApplication(_toolbarButton);
            _toolbarButton = null;
        }

        private void SpawnDialog()
        {
            if (_uiDialog == null) _uiDialog = GenerateDialog();
        }

        private PopupDialog GenerateDialog()
        {
            List<DialogGUIBase> guiItems = new List<DialogGUIBase>();
            if (EditorLogic.fetch == null || EditorLogic.fetch.ship == null)
            {
                guiItems.Add(new DialogGUILabel("No Vessel Detected"));
            }
            else
            {
                guiItems.Add(new DialogGUIToggle(() => _returnTrip, "Return Trip?", delegate { SetReturnTrip(); }));
                for (int i = 0; i < _planets.Count; i++)
                {
                    PlanetDeltaV p = _planets.ElementAt(i);
                    DialogGUIBase[] horizontal = new DialogGUIBase[4];
                    horizontal[0] = new DialogGUILabel(p.Name, GenerateStyle(-1));
                    horizontal[1] = GetDeltaVString(p, "Flyby: ");
                    horizontal[2] = GetDeltaVString(p, "Orbiting: ");
                    horizontal[3] = GetDeltaVString(p, "Landing: ");
                    guiItems.Add(new DialogGUIHorizontalLayout(horizontal));
                }
            }

            guiItems.Add(new DialogGUILabel("*Assuming craft has enough chutes"));
            guiItems.Add(new DialogGUIButton("Close", CloseDialog, false));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new MultiOptionDialog("WhereCanIGoDialog", "", "Where Can I Go", UISkinManager.defaultSkin,
                    _geometry,
                    guiItems.ToArray()), false, UISkinManager.defaultSkin);
        }


        private DialogGUILabel GetDeltaVString(PlanetDeltaV planet, string situation)
        {
            int deltaV = -1;
            string s;
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (situation)
            {
                case "Flyby: ":
                    deltaV = planet.EscapeDv;
                    if (_returnTrip) deltaV += planet.ReturnFromFlybyDv;
                    break;
                case "Orbiting: ":
                    deltaV = planet.OrbitDv;
                    if (_returnTrip) deltaV += planet.ReturnFromOrbitDv;
                    break;
                case "Landing: ":
                    deltaV = planet.LandDv;
                    if (_returnTrip) deltaV += planet.ReturnFromLandingDv;
                    break;
            }

            UIStyle style = GenerateStyle(deltaV);
            string status = VesselStatus(deltaV, situation, planet);
            if (status == "NO")
                status = status + " (" + Math.Ceiling(deltaV - EditorLogic.fetch.ship.vesselDeltaV.TotalDeltaVVac) +
                         "m/s short)";
            if (SituationValid(planet.RelatedBody, situation)) s = " | " + situation + status;
            else s = " | " + situation + "N/A";
            return new DialogGUILabel(s, style);
        }

        private void OnDisable()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(AddToolbarButton);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(AddToolbarButton);
        }

        private static UIStyle GenerateStyle(int deltaV)
        {
            UIStyle style = new UIStyle
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Normal,
                normal = new UIStyleState(),
                fontSize = 12
            };
            style.normal.textColor = GetTextColor(deltaV);
            return style;
        }

        private static bool SituationValid(CelestialBody planetRelatedBody, string situation)
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

        private static string VesselStatus(int deltaVRequired, string situation, PlanetDeltaV planet)
        {
            if (EditorLogic.fetch.ship == null || EditorLogic.fetch.ship.vesselDeltaV == null) return "N/A";
            double vesselDeltaV = EditorLogic.fetch.ship.vesselDeltaV.TotalDeltaVVac;
            if (deltaVRequired < 0) return "N/A";
            if (deltaVRequired > vesselDeltaV) return "NO";
            if (vesselDeltaV - deltaVRequired < deltaVRequired * 0.1) return "MARGINAL";
            if (situation == "Landing: " && planet != null && planet.RequireChutes) return "YES*";
            return "YES";
        }

        private static Color GetTextColor(int deltaV)
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

        private void SetReturnTrip()
        {
            _returnTrip = !_returnTrip;
            CloseDialog();
            Invoke(nameof(SpawnDialog), 0.1f);
        }
    }
}