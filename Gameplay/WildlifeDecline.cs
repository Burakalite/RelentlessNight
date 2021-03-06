﻿using System;
using Harmony;
using UnityEngine;

namespace RelentlessNight
{
    public class Wildlife
    {
        [HarmonyPatch(typeof(SpawnRegion), "Start", new Type[] { })]
        internal static class SpawnRegion_Start_Pre
        {
            private static void Prefix(SpawnRegion __instance)
            {
                if (!RnGlobal.rnActive || GameManager.IsStoryMode()) return;

                bool m_StartHasBeenCalled = __instance.m_StartHasBeenCalled;
                if (m_StartHasBeenCalled) return;

                int glDayNum = RnGlobal.glDayNum;
                float curDayFactor = 1f;
                float curTempFactor = 1f;
                int minPopulation = 1;

                if (RnGlobal.glMinWildlifeAmount < 100)
                {
                    curDayFactor = 1f - Mathf.Clamp((float)glDayNum / RnGlobal.glMinWildlifeDay, 0f, 0.98f) * ((100 - RnGlobal.glMinWildlifeAmount) * 0.01f);
                }
                if (RnGlobal.glWildlifeFreezing)
                {
                    curTempFactor = 1f + Mathf.Clamp((RnGlobal.glOutdoorTempWithoutBlizDrop + 30f) * 0.01f, -0.75f, 0.5f);
                }
                if (RnGlobal.glMinWildlifeAmount == 0)
                {
                    minPopulation = 0;
                }

                __instance.m_ChanceActive *= curDayFactor * curTempFactor;
                __instance.m_MaxRespawnsPerDayPilgrim *= curDayFactor * curTempFactor;
                __instance.m_MaxRespawnsPerDayVoyageur *= curDayFactor * curTempFactor;
                __instance.m_MaxRespawnsPerDayStalker *= curDayFactor * curTempFactor;
                __instance.m_MaxRespawnsPerDayInterloper *= curDayFactor * curTempFactor;
                __instance.m_MaxSimultaneousSpawnsDayPilgrim = Math.Max(minPopulation, Mathf.RoundToInt(__instance.m_MaxSimultaneousSpawnsDayPilgrim * curDayFactor * curTempFactor));
                __instance.m_MaxSimultaneousSpawnsDayVoyageur = Math.Max(minPopulation, Mathf.RoundToInt(__instance.m_MaxSimultaneousSpawnsDayVoyageur * curDayFactor * curTempFactor));
                __instance.m_MaxSimultaneousSpawnsDayStalker = Math.Max(minPopulation, Mathf.RoundToInt(__instance.m_MaxSimultaneousSpawnsDayStalker * curDayFactor * curTempFactor));
                __instance.m_MaxSimultaneousSpawnsDayInterloper = Math.Max(minPopulation, Mathf.RoundToInt(__instance.m_MaxSimultaneousSpawnsDayInterloper * curDayFactor * curTempFactor));
                __instance.m_MaxSimultaneousSpawnsNightPilgrim = Math.Max(minPopulation, Mathf.RoundToInt(__instance.m_MaxSimultaneousSpawnsNightPilgrim * curDayFactor * curTempFactor));
                __instance.m_MaxSimultaneousSpawnsNightVoyageur = Math.Max(minPopulation, Mathf.RoundToInt(__instance.m_MaxSimultaneousSpawnsNightVoyageur * curDayFactor * curTempFactor));
                __instance.m_MaxSimultaneousSpawnsNightStalker = Math.Max(minPopulation, Mathf.RoundToInt(__instance.m_MaxSimultaneousSpawnsNightStalker * curDayFactor * curTempFactor));
                __instance.m_MaxSimultaneousSpawnsNightInterloper = Math.Max(minPopulation, Mathf.RoundToInt(__instance.m_MaxSimultaneousSpawnsNightInterloper * curDayFactor * curTempFactor));

            }
        }
    }
}