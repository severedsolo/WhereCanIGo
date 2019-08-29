using System;
using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using UnityEngine;

namespace WhereCanIGo
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class WhereCanIGoFlight : MonoBehaviour
    {
        private ApplicationLauncherButton _toolbarButton;
        private PopupDialog _uiDialog;
        private readonly Rect _geometry = new Rect(0.5f, 0.5f, 800, 30);
        private bool _returnTrip;
        private Utilities _utilities;

        private void Awake()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(AddToolbarButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(RemoveToolbarButton);
            _utilities = new Utilities();
        }

        private void AddToolbarButton()
        {
            if (_toolbarButton == null)
            {
                _toolbarButton = ApplicationLauncher.Instance.AddModApplication(SpawnDialog, SpawnDialog,
                    null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT,
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
            bool vesselIsHome = FlightGlobals.ActiveVessel.mainBody == FlightGlobals.GetHomeBody();
            if (FlightGlobals.ActiveVessel == null)
            {
                guiItems.Add(new DialogGUILabel("No Vessel Detected"));
            }
            else if(vesselIsHome && FlightGlobals.ActiveVessel.situation == Vessel.Situations.ORBITING)
            {
                guiItems.Add(new DialogGUILabel(_utilities.SystemNotes, _utilities.CreateNoteStyle()));
                guiItems.Add(new DialogGUILabel(_utilities.Warnings, _utilities.CreateNoteStyle()));
                guiItems.Add(new DialogGUIToggle(() => _returnTrip, "Return Trip?", delegate { SetReturnTrip(); }));
                for (int i = 0; i < _utilities.Planets.Count; i++)
                {
                    PlanetDeltaV p = _utilities.Planets.ElementAt(i);
                    DialogGUIBase[] horizontal = new DialogGUIBase[4];
                    horizontal[0] = new DialogGUILabel(p.GetName(), _utilities.GenerateStyle(-1, false));
                    horizontal[1] = GetDeltaVString(p, "Flyby: ");
                    horizontal[2] = GetDeltaVString(p, "Orbiting: ");
                    if(p.IsHomeWorld && p.SynchronousDv != -1) horizontal[3] = GetDeltaVString(p, "Synchronous Orbit: ");
                    else horizontal[3] = GetDeltaVString(p, "Landing: ");
                    guiItems.Add(new DialogGUIHorizontalLayout(horizontal));
                    if (p.SynchronousDv != -1) guiItems.Add(GetDeltaVString(p, "Synchronous Orbit: "));
                }

                guiItems.Add(new DialogGUILabel("*Assuming craft has enough chutes"));
            }
            else
            {
                guiItems.Add(new DialogGUILabel("No Data Available. Achieve stable orbit around "+FlightGlobals.GetHomeBodyName(), _utilities.GenerateStyle(99999, true)));
            }
            guiItems.Add(new DialogGUIButton("Close", () =>_utilities.CloseDialog(_uiDialog), false));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new MultiOptionDialog("WhereCanIGoDialog", "", "Where Can I Go", UISkinManager.defaultSkin,
                    _geometry,
                    guiItems.ToArray()), false, UISkinManager.defaultSkin);
        }

        private PlanetDeltaV ConvertBodyToPlanetDeltaV(CelestialBody cb)
        {
            for (int i = 0; i < _utilities.Planets.Count; i++)
            {
                PlanetDeltaV p = _utilities.Planets.ElementAt(i);
                if (p.Name == cb.bodyName) return p;
            }
            return null;
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
                case "Synchronous Orbit: ":
                    deltaV = planet.SynchronousDv;
                    situation = planet.Name + " " + situation;
                    break;
            }

            deltaV -= ConvertBodyToPlanetDeltaV(FlightGlobals.GetHomeBody()).OrbitDv;
            UIStyle style = _utilities.GenerateStyle(deltaV, false);
            string status = _utilities.VesselStatus(deltaV, situation, planet);
            double shortFallOrDeficit = Math.Abs(deltaV - FlightGlobals.ActiveVessel.VesselDeltaV.TotalDeltaVVac);
            if (status == "NO")
                status = status + " (" + shortFallOrDeficit + "m/s short)";
            else status = status + " (+" + shortFallOrDeficit + "m/s)";
            if (_utilities.SituationValid(planet.RelatedBody, situation)) s = " | " + situation + status;
            else s = " | " + situation + "N/A";
            return new DialogGUILabel(s, style);
        }

        private void OnDisable()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(AddToolbarButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Remove(RemoveToolbarButton);
        }

        private void SetReturnTrip()
        {
            _returnTrip = !_returnTrip;
            _utilities.CloseDialog(_uiDialog);
            Invoke(nameof(SpawnDialog), 0.1f);
        }
    }
}