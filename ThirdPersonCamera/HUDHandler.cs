﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ThirdPersonCamera
{
    public class HUDHandler
    {
        private bool _checkCockpitLockOnNextTick = false;
        private Canvas[] _helmetOffUI;
        private GameObject _helmet;
        private GameObject _lightFlickerEffectBubble;
        private GameObject _darkMatterBubble;
        private OWCamera _lastCamera;

        public HUDHandler()
        {
            GlobalMessenger.AddListener("PutOnHelmet", OnPutOnHelmet);
            GlobalMessenger.AddListener("RemoveHelmet", OnRemoveHelmet);
            GlobalMessenger.AddListener("ExitFlightConsole", OnExitFlightConsole);
            GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", OnEnterFlightConsole);
            GlobalMessenger<OWCamera>.AddListener("SwitchActiveCamera", OnSwitchActiveCamera);
        }

        public void OnDestroy()
        {
            GlobalMessenger.RemoveListener("PutOnHelmet", OnPutOnHelmet);
            GlobalMessenger.RemoveListener("RemoveHelmet", OnRemoveHelmet);
            GlobalMessenger.RemoveListener("ExitFlightConsole", OnExitFlightConsole);
            GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", OnEnterFlightConsole);
            GlobalMessenger<OWCamera>.RemoveListener("SwitchActiveCamera", OnSwitchActiveCamera);
        }

        public void Init()
        {
            _helmetOffUI = GameObject.Find("PlayerHUD/HelmetOffUI")?.GetComponentsInChildren<Canvas>();
            _helmet = GameObject.Find("Helmet");
        }

        private void OnSwitchActiveCamera(OWCamera camera)
        {
            if(camera.Equals(ThirdPersonCamera.OWCamera))
            {
                ShowHelmetHUD(!PlayerState.AtFlightConsole());
                ShowMarkers(true);
                ShowCockpitLockOn(PlayerState.AtFlightConsole());
            }
            else if (_lastCamera.Equals(ThirdPersonCamera.OWCamera))
            {
                ShowHelmetHUD(false);
                ShowMarkers(false);
                ShowCockpitLockOn(false);
            }

            _lastCamera = camera;
        }

        public void OnPutOnHelmet()
        {
            ShowHelmetHUD(Main.IsThirdPerson());
        }

        public void OnRemoveHelmet()
        {
            ShowHelmetHUD(!PlayerState.AtFlightConsole() && Main.IsThirdPerson());
        }

        private void OnExitFlightConsole()
        {
            ShowMarkers(Main.IsThirdPerson());
            ShowHelmetHUD(Main.IsThirdPerson());
            ShowCockpitLockOn(false);
        }

        private void OnEnterFlightConsole(OWRigidbody _)
        {
            ShowHelmetHUD(false);
            ShowMarkers(Main.IsThirdPerson());

            // Set it next frame or it doesnt work sometimes idk
            _checkCockpitLockOnNextTick = true;
            ShowCockpitLockOn(Main.IsThirdPerson());
        }

        private void ShowHelmetHUD(bool visible)
        {
            try
            {
                if (_helmetOffUI != null)
                {
                    foreach (Canvas canvas in _helmetOffUI)
                    {
                        canvas.worldCamera = visible ? ThirdPersonCamera.GetCamera() : Locator.GetPlayerCamera().mainCamera;
                    }
                }

                if (_helmet != null)
                {
                    // Reparent the HUDCamera stuff
                    _helmet.transform.parent = Main.IsThirdPerson() ? ThirdPersonCamera.GetCamera().transform : Locator.GetPlayerCamera().transform;
                    _helmet.transform.localPosition = Vector3.zero;
                }

                // Put bubble effects on the right camera
                if (_darkMatterBubble == null)
                {
                    try
                    {
                        _darkMatterBubble = Locator.GetPlayerCamera().transform.Find("ScreenEffects/DarkMatterBubble").gameObject;
                    }
                    catch (Exception)
                    {
                        Main.WriteWarning("Couldn't find DarkMatterBubble");
                    }
                }
                if (_darkMatterBubble != null)
                {
                    _darkMatterBubble.transform.parent = Main.IsThirdPerson() ? ThirdPersonCamera.GetCamera().transform : Locator.GetPlayerCamera().transform;
                    _darkMatterBubble.transform.localPosition = Vector3.zero;
                }


                if (_lightFlickerEffectBubble == null)
                {
                    try
                    {
                        _lightFlickerEffectBubble = Locator.GetPlayerCamera().transform.Find("ScreenEffects/LightFlickerEffectBubble").gameObject;
                    }
                    catch (Exception)
                    {
                        Main.WriteWarning("Couldn't find LightFlickerEffectBubble");
                    }
                }
                if (_lightFlickerEffectBubble != null)
                {
                    _lightFlickerEffectBubble.transform.parent = Main.IsThirdPerson() ? ThirdPersonCamera.GetCamera().transform : Locator.GetPlayerCamera().transform;
                    _lightFlickerEffectBubble.transform.localPosition = Vector3.zero;
                }
            }
            catch(Exception e)
            {
                Main.WriteError($"{e.StackTrace}, {e.Message}");
            }
        }

        private void ShowMarkers(bool thirdPerson)
        {
            foreach (string s in new string[] { "CanvasMarker(Clone)", "CanvasMarkerManager" })
            {
                Canvas c = GameObject.Find(s)?.GetComponentInChildren<Canvas>();
                if (c != null) c.worldCamera = thirdPerson ? ThirdPersonCamera.GetCamera() : Locator.GetPlayerCamera().mainCamera;
            }
        }

        private void ShowCockpitLockOn(bool visible)
        {
            Canvas c = GameObject.Find("CockpitLockOnCanvas")?.GetComponentInChildren<Canvas>();
            if (c != null) c.worldCamera = visible ? ThirdPersonCamera.GetCamera() : Locator.GetPlayerCamera().mainCamera;
        }

        public void Update()
        {
            if (_checkCockpitLockOnNextTick)
            {
                ShowCockpitLockOn(Main.IsThirdPerson());
            }
        }
    }
}
