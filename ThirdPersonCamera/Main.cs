﻿using OWML.ModHelper;
using OWML.Common;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ThirdPersonCamera
{
    public class Main : ModBehaviour
    {
        private bool loaded = false;
        private bool afterMemoryUplink = false;

        private ThirdPersonCamera _thirdPersonCamera;
        private ScreenTextHandler _screenTextHandler;
        private PlayerMeshHandler _playerMeshHandler;
        private ToolMaterialHandler _toolMaterialHandler;
        private HUDHandler _hudHandler;

        private void Start()
        {
            WriteSuccess($"{nameof(ThirdPersonCamera)} is loaded!");

            // Helpers
            _thirdPersonCamera = new ThirdPersonCamera(this);
            _screenTextHandler = new ScreenTextHandler(this);
            _playerMeshHandler = new PlayerMeshHandler(this);
            _toolMaterialHandler = new ToolMaterialHandler(this);
            _hudHandler = new HUDHandler(this);

            // Patches
            ModHelper.HarmonyHelper.AddPostfix<StreamingGroup>("OnFinishOpenEyes", typeof(Patches), nameof(Patches.EnableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<PlayerCameraEffectController>("CloseEyes", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<PlayerTool>("EquipTool", typeof(Patches), nameof(Patches.EquipTool));
            ModHelper.HarmonyHelper.AddPostfix<PlayerTool>("UnequipTool", typeof(Patches), nameof(Patches.UnequipTool));
            ModHelper.HarmonyHelper.AddPostfix<GhostGrabController>("OnStartLiftPlayer", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<DreamWorldController>("ExitLanternBounds", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<DreamWorldController>("EnterLanternBounds", typeof(Patches), nameof(Patches.EnableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<ProbeLauncher>("RetrieveProbe", typeof(Patches), nameof(Patches.OnRetrieveProbe));
            ModHelper.HarmonyHelper.AddPrefix<MindProjectorTrigger>("OnProjectionStart", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<MindProjectorTrigger>("OnProjectionComplete", typeof(Patches), nameof(Patches.EnableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<MindProjectorTrigger>("OnTriggerVolumeExit", typeof(Patches), nameof(Patches.EnableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<ShipDetachableModule>("Detach", typeof(Patches), nameof(Patches.OnDetach));
            ModHelper.HarmonyHelper.AddPostfix<LanternZoomPoint>("StartZoomIn", typeof(Patches), nameof(Patches.DisableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<LanternZoomPoint>("FinishRetroZoom", typeof(Patches), nameof(Patches.EnableThirdPersonCameraEvent));
            ModHelper.HarmonyHelper.AddPostfix<RoastingStickController>("OnEnterRoastingMode", typeof(Patches), nameof(Patches.OnRoastingStickActivate));

            MethodBase setNomaiText1 = typeof(NomaiTranslatorProp).GetMethod("SetNomaiText", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(NomaiText), typeof(int) }, null);
            ModHelper.HarmonyHelper.AddPostfix(setNomaiText1, typeof(Patches), nameof(Patches.SetNomaiText1));

            MethodBase setNomaiText2 = typeof(NomaiTranslatorProp).GetMethod("SetNomaiText", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(NomaiText) }, null);
            ModHelper.HarmonyHelper.AddPostfix(setNomaiText2, typeof(Patches), nameof(Patches.SetNomaiText2));

            ModHelper.HarmonyHelper.AddPostfix<NomaiTranslatorProp>("SetNomaiAudio", typeof(Patches), nameof(Patches.SetNomaiAudio));

            MethodBase getTextNode = typeof(NomaiText).GetMethod("GetTextNode", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(int) }, null);
            ModHelper.HarmonyHelper.AddPostfix(getTextNode, typeof(Patches), nameof(Patches.GetTextNode));

            ModHelper.HarmonyHelper.AddPostfix<NomaiText>("CheckSetDatabaseCondition", typeof(Patches), nameof(Patches.CheckSetDatabaseCondition));

            // Events
            ModHelper.Events.Subscribe<Flashlight>(Events.AfterStart);

            SceneManager.sceneLoaded += OnSceneLoaded;
            ModHelper.Events.Event += OnEvent;
        }

        public void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ModHelper.Events.Event -= OnEvent;

            _thirdPersonCamera.OnDestroy();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "SolarSystem")
            {
                loaded = false;
                afterMemoryUplink = false;
            } 
            else if (loaded)
            {
                // Already loaded but we're being put into the SolarSystem scene again
                // We must have done the memory uplink (universe is reset after)
                afterMemoryUplink = true;
            }

            _thirdPersonCamera.PreInit();
        }

        private void OnEvent(MonoBehaviour behaviour, Events ev)
        {
            if (behaviour.GetType() == typeof(Flashlight) && ev == Events.AfterStart)
            {
                try
                {
                    _thirdPersonCamera.Init();
                    _screenTextHandler.Init();
                    loaded = true;

                    if (afterMemoryUplink) _thirdPersonCamera.CameraEnabled = true;

                    // This actually doesn't seem to affect the player camera
                    GameObject helmetMesh = GameObject.Find("Traveller_Mesh_v01:PlayerSuit_Helmet");
                    helmetMesh.layer = 0;

                    GameObject probeLauncher = Locator.GetPlayerBody().GetComponentInChildren<ProbeLauncher>().gameObject;
                    probeLauncher.layer = 0;

                }
                catch (Exception)
                {
                    WriteError("Init failed");
                }
            }
        }

        private void Update()
        {
            if (!loaded) return;

            _thirdPersonCamera.Update();
            _playerMeshHandler.Update();
            _toolMaterialHandler.Update();
        }

        public bool IsThirdPerson()
        {
            return _thirdPersonCamera.CameraEnabled && _thirdPersonCamera.CameraActive;
        }

        public void WriteError(string msg)
        {
            ModHelper.Console.WriteLine(msg, MessageType.Error);
        }

        public void WriteWarning(string msg)
        {
            ModHelper.Console.WriteLine(msg, MessageType.Warning);
        }

        public void WriteInfo(string msg)
        {
            ModHelper.Console.WriteLine(msg, MessageType.Info);
        }

        public void WriteSuccess(string msg)
        {
            ModHelper.Console.WriteLine(msg, MessageType.Success);
        }
    }
}
