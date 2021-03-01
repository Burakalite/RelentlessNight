﻿using Harmony;
using RelentlessNight;
using UnityEngine;

namespace RelentlessNight
{
    public class TimeChange
    {
        [HarmonyPatch(typeof(Feat_EfficientMachine), "IncrementElapsedHours", null)]
        internal static class Feat_EfficientMachine_IncrementElapsedHours_Pre
        {
            private static void Prefix(Feat_EfficientMachine __instance, ref float hours)
            {
                if (!RnGl.rnActive) return;

                if (GameManager.m_IsPaused) return;

                hours = RnGl.rnHours;
            }
        }

        [HarmonyPatch(typeof(Panel_MainMenu), "OnNewSandbox", null)]
        internal class Panel_MainMenu_OnNewSandbox_Pre
        {
            private static void Prefix(Panel_MainMenu __instance)
            {
                if (!RnGl.rnActive) return;

                RnGl.glDayTidallyLocked = -1;
                RnGl.glDayNum = 1;
                RnGl.rnIndoorTempFactor = 1f;
                RnGl.rnFireShouldHeatWholeScene = false;
                RnGl.rnHeatDissapationFactor = 15f;
                RnGl.glIsCarryingCarcass = false;
            }
        }

        [HarmonyPatch(typeof(PlayerManager), "TeleportPlayerAfterSceneLoad", null)]
        internal static class PlayerManager_TeleportPlayerAfterSceneLoad_Post
        {
            private static void Postfix()
            {
                if (!RnGl.rnActive) return;

                if (RnGl.glEndgameActive && RnGl.glEndgameDay == 0) GameManager.GetTimeOfDayComponent().SetNormalizedTime(0f);
            }
        }

        [HarmonyPatch(typeof(UniStormWeatherSystem), "SetNormalizedTime", new[] { typeof(float), typeof(bool) })]
        internal static class UniStormWeatherSystem_SetNormalizedTime_Pre
        {
            private static bool Prefix(UniStormWeatherSystem __instance, ref float time)
            {
                if (!RnGl.rnActive) return true;

                float m_DeltaTime = __instance.m_DeltaTime;
                RnGl.rnHours = m_DeltaTime / Mathf.Clamp((7200f * __instance.m_DayLengthScale / RnGl.rnTimeAccel) * GameManager.GetExperienceModeManagerComponent().GetTimeOfDayScale(), 1f, float.PositiveInfinity) * 24f;

                if (RnGl.glEndgameActive && __instance.m_DayCounter >= RnGl.glEndgameDay && GameManager.GetTimeOfDayComponent().IsNight())
                {
                    if (RnGl.glDayTidallyLocked == -1) RnGl.glDayTidallyLocked = __instance.m_DayCounter;
                    time = __instance.m_NormalizedTime;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(TimeWidget), "Update", null)]
        internal static class TimeWidget_Start_Post
        {
            private static void Postfix(TimeWidget __instance)
            {
                if (RnGl.rnActive && RnGl.glEndgameActive && RnGl.glDayTidallyLocked != -1 && RnGl.glDayTidallyLocked > Settings.options.coEndgameDay)
                {
                    __instance.m_MoonSprite.color = new Color(0.2f, 0.2f, 0.6f, 1f);
                }
                else
                {
                    __instance.m_MoonSprite.color = Color.white;
                }
            }
        }

        [HarmonyPatch(typeof(UniStormWeatherSystem), "UpdateTimeStats", null)]
        internal static class UniStormWeatherSystem_UpdateTimeStats_Pre
        {
            private static void Prefix(UniStormWeatherSystem __instance, ref float timeDeltaHours)
            {
                if (!RnGl.rnActive || GameManager.m_IsPaused || !__instance.m_MainCamera) return;

                timeDeltaHours = RnGl.rnHours;
            }
        }

        [HarmonyPatch(typeof(UniStormWeatherSystem), "Update", null)]
        internal static class UniStormWeatherSystem_Update_Pre
        {
            private static void Prefix(UniStormWeatherSystem __instance)
            {
                if (!RnGl.rnActive || GameManager.m_IsPaused || !__instance.m_MainCamera) return;

                float dayLengthFactor = (float)RnGl.glRotationDecline * 72f;
                float curDay = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused() / 24f;
                __instance.m_DayLength = ((7200f + curDay * dayLengthFactor) / RnGl.rnTimeAccel) * GameManager.GetExperienceModeManagerComponent().GetTimeOfDayScale();

                RnGl.rnElapsedHoursAccumulator = __instance.m_ElapsedHoursAccumulator;
                RnGl.rnElapsedHours = __instance.m_ElapsedHours;
            }
        }

        [HarmonyPatch(typeof(UniStormWeatherSystem), "Update", null)]
        internal static class UniStormWeatherSystem_Update_Post
        {
            private static void Postfix(UniStormWeatherSystem __instance)
            {
                if (!RnGl.rnActive || GameManager.m_IsPaused || !__instance.m_MainCamera) return;

                RnGl.rnElapsedHoursAccumulator += RnGl.rnHours;

                if (RnGl.rnElapsedHoursAccumulator > 0.5f)
                {
                    RnGl.rnElapsedHours += RnGl.rnElapsedHoursAccumulator;
                    RnGl.rnElapsedHoursAccumulator = 0f;
                    __instance.SetMoonPhase();
                }

                __instance.m_ElapsedHoursAccumulator = RnGl.rnElapsedHoursAccumulator;
                __instance.m_ElapsedHours = RnGl.rnElapsedHours;

                __instance.m_DayCounter = 1 + (int)(GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused() / 24f);
                RnGl.glDayNum = __instance.m_DayCounter;
                __instance.m_DayLength = (7200f / RnGl.rnTimeAccel) * GameManager.GetExperienceModeManagerComponent().GetTimeOfDayScale();
            }
        }
    }
}