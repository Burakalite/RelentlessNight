﻿using System;
using UnityEngine;
using Harmony;

namespace RelentlessNight
{
    public class HeatRetention
    {
        [HarmonyPatch(typeof(GameManager), "CacheComponents", null)]
        internal static class GameManager_CacheComponents_Pos
        {
            private static void Postfix(GameManager __instance)
            {
                //Debug.Log("FireManager_Start_Pos");
                if (!RnGl.rnActive) return;

                GameManager.GetFireManagerComponent().m_MaxHeatIncreaseOfFire = 120f;
                GameManager.GetFireManagerComponent().m_TakeTorchReduceBurnMinutes *= RnGl.glFireFuelFactor;
            }
        }

        [HarmonyPatch(typeof(Panel_FeedFire), "OnFeedFire", null)]
        public class Panel_Panel_FeedFire_Pre
        {
            private static bool Prefix(Panel_FeedFire __instance)
            {
                //Debug.Log("Panel_Panel_FeedFire_Pre");
                if (!RnGl.rnActive || !RnGl.glHeatRetention) return true;

                if (__instance.ProgressBarIsActive())
                {
                    GameAudioManager.PlayGUIError();
                    return false;
                }
                GearItem selectedFuelSource = __instance.GetSelectedFuelSource();
                if (selectedFuelSource == null)
                {
                    GameAudioManager.PlayGUIError();
                    return false;
                }
                FuelSourceItem fuelSourceItem = selectedFuelSource.m_FuelSourceItem;
                if (fuelSourceItem == null)
                {
                    GameAudioManager.PlayGUIError();
                    return false;
                }
                GameObject m_FireContainer = __instance.m_FireContainer;
                if (!m_FireContainer)
                {
                    GameAudioManager.PlayGUIError();
                    return false;
                }
                Fire m_Fire = __instance.m_Fire;
                if (!m_Fire)
                {
                    return false;
                }
                if (m_Fire.FireShouldBlowOutFromWind())
                {
                    HUDMessage.AddMessage(Localization.Get("GAMEPLAY_TooWindyToAddFuel"), false);
                    GameAudioManager.PlayGUIError();
                    return false;
                }

                bool FireInForge = __instance.FireInForge();
                if (!FireInForge)
                {
                    float num = fuelSourceItem.GetModifiedBurnDurationHours(selectedFuelSource.GetNormalizedCondition()) * 60f;
                    float num2 = m_Fire.GetRemainingLifeTimeSeconds() / 60f;
                    float num3 = (num + num2) / 60f;
                    if (num3 > GameManager.GetFireManagerComponent().m_MaxDurationHoursOfFire && m_Fire.GetCurrentTempIncrease() == m_Fire.m_HeatSource.m_MaxTempIncrease)
                    {
                        GameAudioManager.PlayGUIError();
                        HUDMessage.AddMessage(Localization.Get("GAMEPLAY_CannotAddMoreFuel"), false);
                        return false;
                    }
                }
                if (selectedFuelSource.m_ResearchItem && !selectedFuelSource.m_ResearchItem.IsResearchComplete())
                {
                    __instance.m_ResearchItemToBurn = selectedFuelSource;

                    InterfaceManager.m_Panel_Confirmation.ShowBurnResearchNotification(new Action(() => __instance.ForceBurnResearchItem()));
                    return false;
                }
                GameAudioManager.PlaySound(__instance.m_FeedFireAudio, InterfaceManager.GetSoundEmitter());
                m_Fire.AddFuel(selectedFuelSource, FireInForge);
                GameManager.GetPlayerManagerComponent().ConsumeUnitFromInventory(fuelSourceItem.gameObject);

                return false;
            }
        }
        public delegate void ForceBurnResearchItem();

        [HarmonyPatch(typeof(Fire), "AddFuel", null)]
        public class Fire_AddFuel_Pos
        {
            private static void Postfix(Fire __instance)
            {
                //Debug.Log("Fire_AddFuel_Pos");
                if (!RnGl.rnActive || !RnGl.glHeatRetention) return;

                __instance.m_HeatSource.m_MaxTempIncreaseInnerRadius = 1000f;
                __instance.m_HeatSource.m_MaxTempIncreaseOuterRadius = 1000f;
            }
        }

        [HarmonyPatch(typeof(Fire), "Deserialize", null)]
        public class Fire_Deserialize_Pos
        {
            private static void Postfix(Fire __instance)
            {
                //Debug.Log("Fire_Deserialize_Pos");
                if (!RnGl.rnActive || !RnGl.glHeatRetention) return;

                float m_ElapsedOnTODSeconds = __instance.m_ElapsedOnTODSeconds;
                float m_MaxOnTODSeconds = __instance.m_MaxOnTODSeconds;
                FireState m_FireState = __instance.m_FireState;

                float hourAfterburnOut = (m_ElapsedOnTODSeconds - m_MaxOnTODSeconds) / 3600f;
                float heatRemaining = __instance.m_HeatSource.m_MaxTempIncrease - hourAfterburnOut * 2.5f;

                if (m_FireState == FireState.Off && __instance.m_HeatSource.m_MaxTempIncrease > 0f && m_MaxOnTODSeconds > 0f)
                {
                    if (heatRemaining > 0f)
                    {
                        __instance.m_HeatSource.m_TempIncrease = heatRemaining;
                        GameManager.GetHeatSourceManagerComponent().AddHeatSource(__instance.m_HeatSource);
                    }
                }

            }
        }

        [HarmonyPatch(typeof(Fire), "TurnOn", null)]
        public class Fire_TurnOn_Pos
        {
            private static void Postfix(Fire __instance)
            {
                //Debug.Log("Fire_TurnOn_Pos");
                if (!RnGl.rnActive || !RnGl.glHeatRetention) return;

                __instance.m_HeatSource.m_MaxTempIncrease += RnGl.rnCurrentRetainedHeat;
                __instance.m_FuelHeatIncrease = __instance.m_HeatSource.m_MaxTempIncrease;

                if (RnGl.rnFireShouldHeatWholeScene)
                {
                    __instance.m_HeatSource.m_MaxTempIncreaseInnerRadius = 1000f;
                    __instance.m_HeatSource.m_MaxTempIncreaseOuterRadius = 1000f;
                }

            }
        }

        [HarmonyPatch(typeof(Fire), "TurnOn", null)]
        public class Fire_TurnOn_Pre
        {
            private static void Prefix(Fire __instance, ref bool maskTempIncrease)
            {
                //Debug.Log("Fire_TurnOn_Pre");
                if (!RnGl.rnActive || !RnGl.glHeatRetention) return;
                maskTempIncrease = false;

                if (!GameManager.GetFireManagerComponent().PointInRadiusOfBurningFire(GameManager.GetPlayerTransform().position))
                {
                    RnGl.rnCurrentRetainedHeat = GameManager.GetHeatSourceManagerComponent().GetTemperatureIncrease(GameManager.GetPlayerTransform().position);
                }
            }
        }

        // Makes sure to track how long has passed even after the fire has gone out, for future calculation on how much heat remains
        [HarmonyPatch(typeof(Fire), "Update", null)]
        public class Fire_Update_Pre
        {
            private static void Prefix(Fire __instance)
            {
                //Debug.Log("Fire_Update_Pre");
                if (!RnGl.rnActive || !RnGl.glHeatRetention || GameManager.m_IsPaused) return;

                // Temp Bug Fix1
                if (__instance.m_HeatSource.m_MaxTempIncreaseInnerRadius == 1000f || __instance.m_HeatSource.m_MaxTempIncreaseOuterRadius == 1000f)
                {
                    __instance.m_HeatSource.m_MaxTempIncreaseInnerRadius = 2;
                    __instance.m_HeatSource.m_MaxTempIncreaseOuterRadius = 30;
                }

                FireState fireState = __instance.m_FireState;
                if (fireState == FireState.Off)
                {
                    //float m_ElapsedOnTODSeconds = __instance.m_ElapsedOnTODSeconds;
                    //m_ElapsedOnTODSeconds += GameManager.GetTimeOfDayComponent().GetTODSeconds(Time.deltaTime);
                    //__instance.m_ElapsedOnTODSeconds = m_ElapsedOnTODSeconds;

                    __instance.m_ElapsedOnTODSeconds += GameManager.GetTimeOfDayComponent().GetTODSeconds(Time.deltaTime);
                }
            }
        }

        // Makes sure if the fire still has retained heat, that the heatsource is not turned off
        /*
        [HarmonyPatch(typeof(HeatSource), "TurnOff", null)] //inlined everywhere, HeatSource.TurnOffImmediate?
        public class HeatSource_TurnOff_Pre
        {
            private static bool Prefix(HeatSource __instance)
            {
                //Debug.Log("HeatSource_TurnOff_Pre");
                if (!RnGl.rnActive || !RnGl.glHeatRetention) return true;    

                FireState fireState = (FireState)AccessTools.Field(typeof(Fire), "m_FireState").GetValue(__instance);
                float m_MaxOnTODSeconds = (float)AccessTools.Field(typeof(Fire), "m_MaxOnTODSeconds").GetValue(__instance);

                if (fireState == FireState.Off && __instance.m_MaxTempIncrease > 0f && m_MaxOnTODSeconds > 0f)
                {
                    float m_ElapsedOnTODSeconds = (float)AccessTools.Field(typeof(Fire), "m_ElapsedOnTODSeconds").GetValue(__instance);
                    float hoursAfterBurnOut = (m_ElapsedOnTODSeconds - m_MaxOnTODSeconds) / 3600f;
                    float tempBonusRemaining = __instance.m_MaxTempIncrease - hoursAfterBurnOut * 2.5f * RnGl.rnHeatDissapationFactor;

                    if (tempBonusRemaining > 0f)
                    {
                        return false;
                    }
                }
                return true;      
            }
        }
        */

        [HarmonyPatch(typeof(HeatSource), "TurnOffImmediate", null)]
        public class HeatSource_TurnOffImmediate_Pre
        {
            private static bool Prefix(HeatSource __instance)
            {
                //Debug.Log("HeatSource_TurnOffImmediate_Pre");
                if (!RnGl.rnActive || !RnGl.glHeatRetention) return true;

                if (__instance.m_TempIncrease > 0f) return false;
                return true;
            }
        }

        // Changes the temperature decline rate after a fire has gone out, this simulates the heat retention
        [HarmonyPatch(typeof(HeatSource), "Update", null)]
        public class HeatSource_Update_Pre
        {
            private static bool Prefix(HeatSource __instance)
            {
                //Debug.Log("HeatSource_Update_Pre");
                if (!RnGl.rnActive || !RnGl.glHeatRetention) return true;

                if (GameManager.m_IsPaused) return false;

                float m_TempIncrease = __instance.m_TempIncrease;

                if (Mathf.Approximately(__instance.m_TimeToReachMaxTempMinutes, 0f))
                {
                    m_TempIncrease = __instance.m_MaxTempIncrease;
                }
                else
                {
                    float todminutes = GameManager.GetTimeOfDayComponent().GetTODMinutes(Time.deltaTime);
                    bool m_TurnedOn = __instance.m_TurnedOn;

                    if (m_TurnedOn)
                    {
                        m_TempIncrease += todminutes / __instance.m_TimeToReachMaxTempMinutes * __instance.m_MaxTempIncrease;
                    }
                    else
                    {
                        m_TempIncrease -= todminutes / 24f * RnGl.rnHeatDissapationFactor;
                    }

                    if (__instance.m_MaxTempIncrease > 0f)
                    {
                        m_TempIncrease = Mathf.Clamp(m_TempIncrease, 0f, __instance.m_MaxTempIncrease);
                    }
                    else
                    {
                        m_TempIncrease = Mathf.Clamp(m_TempIncrease, __instance.m_MaxTempIncrease, 0f);
                    }
                }
                __instance.m_TempIncrease = m_TempIncrease;

                return false;
            }
        }
    }
}