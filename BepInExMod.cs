using BepInEx;
using BepInEx.Logging;

using HarmonyLib;

using System;
using System.Reflection;

using UnityEngine;


namespace IronRebellionTelemetry
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    internal class BepInExPlugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Log;

        public static float velocityX, velocityY, velocityZ;
        public static float angularX, angularY, angularZ;
        public static float rotationX, rotationY, rotationZ;
        public static float rumbleIntensity = 0f;

        public static float currentTilt = 0f;
        public static float currentLean = 0f;

        public static bool isFlying = false;
        public static bool isRunning = false;
        public static bool isHit = false;
        public static bool mechOn = false;
        public static bool stage4Booted = false;

        public static bool gameRunning = false;

        // Variables which are very shortly true, needs to be checked that the thread read it true value at least once
        public static bool stompedSend = false;
        public static bool landedSend = false;
        public static bool jumpedSend = false;
        public static bool weaponFiredSend = false;

        public static bool weaponFired = false;
        public static bool stomped = false;
        public static bool landed = false;
        public static bool jumped = false;

        private void Awake()
        {
            BepInExPlugin.Log = base.Logger;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Log.LogInfo("Finished patching.");
            TelemetrySender.Start();
            Log.LogInfo("Started Telemetry Sender.");
        }

        // RB Telemetry

        [HarmonyPatch(typeof(CockpitAnimationManager))]
        public class GetVelocityPatch
        {
            private static readonly FieldInfo currentTiltField = AccessTools.Field(typeof(CockpitAnimationManager), "currentTilt");
            //private static readonly FieldInfo animField = AccessTools.Field(typeof(CockpitAnimationManager), "anim");

            [HarmonyPatch(typeof(CockpitAnimationManager), "Update", new Type[] { })]
            [HarmonyPostfix]
            private static void PostfixUpdate(CockpitAnimationManager __instance)
            {
                Rigidbody cockpitRB = __instance.cockpitRB;
                Transform cockpitTransform = cockpitRB.transform;
                //Animator anim = (Animator)animField.GetValue(__instance);
                //bool isBooting = anim.GetBool("Stage_4");

                Vector3 worldVelocity;
                Vector3 worldAngularVelocity;

                // Convert to local space
                Vector3 localVelocity = cockpitTransform.InverseTransformDirection(cockpitRB.velocity);
                Vector3 localAngularVelocity = cockpitTransform.InverseTransformDirection(cockpitRB.angularVelocity);

                BepInExPlugin.velocityX = localVelocity.x;
                BepInExPlugin.velocityY = localVelocity.y;
                BepInExPlugin.velocityZ = localVelocity.z;

                BepInExPlugin.angularX = localAngularVelocity.x;
                BepInExPlugin.angularY = localAngularVelocity.y;
                BepInExPlugin.angularZ = localAngularVelocity.z;
 
                BepInExPlugin.rotationX = NormalizeAngle(cockpitTransform.rotation.eulerAngles.x);
                BepInExPlugin.rotationY = cockpitTransform.rotation.eulerAngles.y;
                BepInExPlugin.rotationZ = NormalizeAngle(cockpitTransform.rotation.eulerAngles.z);
                
                //Accessing private field
                BepInExPlugin.currentTilt = (float)currentTiltField.GetValue(__instance);

                //Reset signals
                if(BepInExPlugin.stompedSend){
                    BepInExPlugin.stomped = false;
                    BepInExPlugin.stompedSend = false;
                }

                if (BepInExPlugin.landedSend)
                {
                    BepInExPlugin.landed = false;
                    BepInExPlugin.landedSend = false;
                }

                if (BepInExPlugin.jumpedSend)
                {
                    BepInExPlugin.jumped = false;
                    BepInExPlugin.jumpedSend = false;
                }

                if (BepInExPlugin.weaponFiredSend)
                {
                    BepInExPlugin.weaponFired = false;
                    BepInExPlugin.weaponFiredSend = false;
                }

                // Reset rotation if inside lobby
                if (!PlayerRig.rigInstance.transform.parent)
                {
                    rotationX = 0f;
                    rotationZ = 0f;
                }
            }

            private static float NormalizeAngle(float angle)
            {
                return (angle > 180f) ? angle - 360f : angle;
            }

            [HarmonyPatch(typeof(CockpitAnimationManager), "SetLean", new Type[] { typeof(float) })]
            [HarmonyPrefix]
            private static void Prefix(ref float speedScale, CockpitAnimationManager __instance)
            {
                BepInExPlugin.currentLean = __instance.rJoystick.horizontal * speedScale * __instance.leanMultiplier * Mathf.Sign(__instance.rJoystick.horizontal);
            }

            [HarmonyPatch(typeof(CockpitAnimationManager), "JumpAnimation", new Type[] { })]
            [HarmonyPostfix]
            private static void PostfixJumpAnimation()
            {
                BepInExPlugin.jumped = true;
            }
        }


        // Shaking

        [HarmonyPatch(typeof(CockpitAnimationSounds), "PlayStompSound")]
        public class GetShakePatch
        {
            [HarmonyPatch(typeof(CockpitAnimationSounds), "PlayStompSound")]
            [HarmonyPostfix]
            private static void PostfixPlayStompSound()
            {
                BepInExPlugin.stomped = true;
            }

            [HarmonyPatch(typeof(CockpitAnimationSounds), "StartFlyingSound")]
            [HarmonyPostfix]
            private static void PostfixStartFlyingSound()
            {
                BepInExPlugin.isFlying = true;
            }

            [HarmonyPatch(typeof(CockpitAnimationSounds), "EndFlyingSound")]
            [HarmonyPostfix]
            private static void PostfixEndFlyingSound()
            {
                BepInExPlugin.isFlying = false;
            }

            [HarmonyPatch(typeof(CockpitAnimationSounds), "StartSprintSound")]
            [HarmonyPostfix]
            private static void PostfixStartSprintSound()
            {
                BepInExPlugin.isRunning = true;
            }

            [HarmonyPatch(typeof(CockpitAnimationSounds), "EndSprintSound")]
            [HarmonyPostfix]
            private static void PostfixEndSprintSound()
            {
                BepInExPlugin.isRunning = false;
            }

            [HarmonyPatch(typeof(CockpitAnimationSounds), "PlayLandingSound")]
            [HarmonyPostfix]
            private static void PostfixPlayLandingSound()
            {
                BepInExPlugin.landed = true;
            }
        }

        [HarmonyPatch(typeof(CockpitHitter))]
        public class CockpitShakePatch
        {
            private static readonly FieldInfo isHitField = AccessTools.Field(typeof(CockpitHitter), "isHit");
            //private static readonly FieldInfo weaponFiredField = AccessTools.Field(typeof(CockpitHitter), "weaponFired");

            [HarmonyPatch(typeof(CockpitHitter), "Update")]
            [HarmonyPostfix]
            public static void PostfixUpdate(CockpitHitter __instance)
            {
                // Weapon fired is too long true: Until the weapon reloads fully. I only want the inital impule
                //BepInExPlugin.weaponFired = (bool)weaponFiredField.GetValue(__instance);

                BepInExPlugin.isHit = (bool)isHitField.GetValue(__instance);
            }

            [HarmonyPatch(typeof(CockpitHitter), "FireWeapon", new Type[] { typeof(int) })]
            [HarmonyPostfix]
            public static void Postfix()
            {
                BepInExPlugin.weaponFired = true;
            }
        }

        /*
        // If a game ends reset the rotation to zero (otherwise it can happen that you hanging in the chair in death position while in the menu

        [HarmonyPatch(typeof(Match_Gamemode_Conquest), "GameEnd")]
        public class ConquestEndDetector
        {
            [HarmonyPrefix]
            public static void PrefixGameEnd(Match_Gamemode_Conquest __instance)
            {
                if (!__instance.exiting)
                {
                    rotationX = 0f;
                    rotationZ = 0f;
                }
            }
        }

        [HarmonyPatch(typeof(Match_Gamemode_DataCap), "GameEnd")]
        public class DataCapEndDetector
        {
            [HarmonyPrefix]
            public static void PrefixGameEnd(Match_Gamemode_DataCap __instance)
            {
                if (!__instance.exiting)
                {
                    rotationX = 0f;
                    rotationZ = 0f;
                }
            }
        }

        [HarmonyPatch(typeof(Match_Gamemode_StrikePoint), "GameEnd")]
        public class StrikePointndDetector
        {
            [HarmonyPrefix]
            public static void PrefixGameEnd(Match_Gamemode_StrikePoint __instance)
            {
                if (!__instance.exiting)
                {
                    rotationX = 0f;
                    rotationZ = 0f;
                }
            }
        }

        [HarmonyPatch(typeof(Match_Gamemode_TeamDeathmatch), "GameEnd")]
        public class TeamDeathmatchDetector
        {
            [HarmonyPrefix]
            public static void PrefixGameEnd(Match_Gamemode_TeamDeathmatch __instance)
            {
                if (!__instance.exiting)
                {
                    rotationX = 0f;
                    rotationZ = 0f;
                }
            }
        }

        [HarmonyPatch(typeof(Match_Gamemode_Team_PointCapture), "GameEnd")]
        public class Team_PointCaptureDetector
        {
            [HarmonyPrefix]
            public static void PrefixGameEnd(Match_Gamemode_Team_PointCapture __instance)
            {
                if (!__instance.exiting)
                {
                    rotationX = 0f;
                    rotationZ = 0f;
                }
            }
        }
        */

        /*
        [HarmonyPatch(typeof(ControlPanelControls))]
        public class GetBootingPatch
        {
            private static readonly FieldInfo mechOnField = AccessTools.Field(typeof(ControlPanelControls), "mechOn");

            [HarmonyPatch(typeof(ControlPanelControls), "Update")]
            [HarmonyPostfix]
            public static void PostfixUpdate(ControlPanelControls __instance)
            {
                BepInExPlugin.mechOn = (bool)mechOnField.GetValue(__instance);
            }

            [HarmonyPatch(typeof(ControlPanelControls), "MechBootUpStage4")]
            [HarmonyPostfix]
            public static void PostfixMechBootUpStage4(ControlPanelControls __instance)
            {
                BepInExPlugin.stage4Booted = true;
            }
        }
        */
    }
}
