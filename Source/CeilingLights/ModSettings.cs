using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using System.Reflection;
using System;

namespace CeilingLights
{
  public class CeilingLightsSettings : ModSettings
  {
    public const float OverlightRadiusMin = 2;
    public const float OverlightRadiusMax = 30;
    private const float DefaultOverlightRadius = 16;

    public const float LightRadiusMin = 2;
    public const float LightRadiusMax = 30;
    private const float DefaultLightRadius = 20;

    public const int PowerConsumptionMin = 10;
    public const int PowerConsumptionMax = 4000;
    private const int DefaultPowerConsumption = 1000;

    public static int growLPC = DefaultPowerConsumption;
    public static float growLightOverlightRadius = DefaultOverlightRadius;
    public static float growLightRadius = DefaultLightRadius;
    public static string growLPCTooltip = ""; // Cant translate CLSettingsGrowLPCTooltip for some reason

    public override void ExposeData()
    {
      base.ExposeData();
      Scribe_Values.Look(ref growLPC, "growLightPowerConsumption", DefaultPowerConsumption);

      Scribe_Values.Look(ref growLightOverlightRadius, "growLightOverlightRadius", DefaultOverlightRadius);
      growLightOverlightRadius = Mathf.Clamp(growLightOverlightRadius, OverlightRadiusMin, OverlightRadiusMax);

      Scribe_Values.Look(ref growLightRadius, "growLightRadius", DefaultLightRadius);
      growLightRadius = Mathf.Clamp(growLightRadius, Math.Max(LightRadiusMin, growLightOverlightRadius), LightRadiusMax);

      if (Scribe.mode == LoadSaveMode.PostLoadInit)
      {
        ApplySettings();
      }
    }

    public static void ApplySettings()
    {
      ThingDef growLightDef = DefDatabase<ThingDef>.GetNamedSilentFail("Lighting_CeilingGrowLight");
      if (growLightDef == null)
      {
        return;
      }

      CompProperties_Glower glower = growLightDef.comps?.OfType<CompProperties_Glower>().FirstOrDefault();
      if (glower != null)
      {
        growLightOverlightRadius = Mathf.Clamp(growLightOverlightRadius, OverlightRadiusMin, OverlightRadiusMax);
        growLightRadius = Mathf.Clamp(growLightRadius, Math.Max(LightRadiusMin, growLightOverlightRadius), LightRadiusMax);
        glower.overlightRadius = growLightOverlightRadius;
        glower.glowRadius = growLightRadius;
        growLightDef.specialDisplayRadius = growLightOverlightRadius - 1.01f - (Mathf.Pow((growLightOverlightRadius - 3), 1.1f) * 0.065f);
      }

      CompProperties_Power power = growLightDef.comps?.OfType<CompProperties_Power>().FirstOrDefault();
      if (power != null)
      {
        var field = typeof(CompProperties_Power).GetField("basePowerConsumption", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
          field.SetValue(power, (float)growLPC);
        }
      }
    }
  }

  public class CeilingLightsMod : Mod
  {
    CeilingLightsSettings settings;
    public CeilingLightsMod(ModContentPack con) : base(con)
    {
      this.settings = GetSettings<CeilingLightsSettings>();
      LongEventHandler.ExecuteWhenFinished(CeilingLightsSettings.ApplySettings);
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
      Listing_Standard listing = new Listing_Standard();
      listing.Begin(inRect);

      DrawSlider(listing, CeilingLightsSettings.growLightOverlightRadius, "CLSettingsOverlightRadius", "CLSettingsOverlightRadiusTip", CeilingLightsSettings.OverlightRadiusMin, CeilingLightsSettings.OverlightRadiusMax, 0.1f, overlightValue =>
      {
        if (overlightValue != CeilingLightsSettings.growLightOverlightRadius)
        {
          CeilingLightsSettings.growLightOverlightRadius = overlightValue;
          CeilingLightsSettings.ApplySettings();
        }
      });

      listing.Gap();

      DrawSlider(listing, CeilingLightsSettings.growLightRadius, "CLSettingsLightRadius", "CLSettingsLightRadiusTip", CeilingLightsSettings.LightRadiusMin, CeilingLightsSettings.LightRadiusMax, 0.1f, lightValue =>
      {
        if (lightValue != CeilingLightsSettings.growLightRadius)
        {
          CeilingLightsSettings.growLightRadius = lightValue;
          CeilingLightsSettings.ApplySettings();
        }
      });

      listing.Gap();

      DrawSlider(listing, CeilingLightsSettings.growLPC, "CLSettingsGrowLPC", "CLSettingsGrowLPCTooltip", CeilingLightsSettings.PowerConsumptionMin, CeilingLightsSettings.PowerConsumptionMax, 10f, powerSliderValue =>
      {
        int powerValue = Mathf.RoundToInt(powerSliderValue);
        if (powerValue != CeilingLightsSettings.growLPC)
        {
          CeilingLightsSettings.growLPC = powerValue;
          CeilingLightsSettings.ApplySettings();
        }
      });

      listing.End();
      base.DoSettingsWindowContents(inRect);
    }

    private void DrawSlider(Listing_Standard listing, float value, string labelId, string tipId, float min, float max, float roundTo, Action<float> updateFunc)
    {
      Rect labelRect = listing.GetRect(Text.LineHeight);
      Widgets.Label(labelRect, labelId.Translate(value.ToString("0.0")));
      TooltipHandler.TipRegion(labelRect, tipId.Translate());
      Rect sliderRect = listing.GetRect(22f);
      float updatedValue = Widgets.HorizontalSlider(sliderRect, value, min, max, true, null, min.ToString(), max.ToString(), roundTo);
      updateFunc(updatedValue);
    }

    public override void WriteSettings()
    {
      base.WriteSettings();
      CeilingLightsSettings.ApplySettings();
    }

    public override string SettingsCategory()
    {
      // This is the title on the mod screen
      return "CLSettingsCategory".Translate();
    }
  }
}
