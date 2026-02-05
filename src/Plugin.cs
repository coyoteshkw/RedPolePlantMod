using BepInEx;
using BepInEx.Logging;
using System.Security.Permissions;
using UnityEngine;
using Menu.Remix.MixedUI;
using RWCustom;
using System.Collections.Generic;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace RedPolePlantMod
{
    [BepInPlugin("com.coyoteshkw.RedPolePlantMod", "RedPolePlantMod", "0.1.1")]
    public class RedMimic : BaseUnityPlugin
    {
        public static RedMimicOptions Options;
        private bool _initialized = false;

        public void OnEnable()
        {
            Logger.LogInfo("RedMimic: OnEnable called.");
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            On.PoleMimicGraphics.DrawSprites += PoleMimicGraphics_DrawSprites;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            try
            {
                orig(self);
            }
            catch (System.Exception ex)
            {
                Logger.LogError("RedMimic: Error in orig(self) OnModsInit: " + ex);
            }

            if (_initialized) return;
            _initialized = true;

            Logger.LogInfo("RedMimic: Initializing Options...");

            try
            {
                Options = new RedMimicOptions(this, Logger);
                MachineConnector.SetRegisteredOI("com.coyoteshkw.RedPolePlantMod", Options);
                Logger.LogInfo("RedMimic: Successfully registered options with MachineConnector.");
            }
            catch (System.Exception ex)
            {
                Logger.LogError("RedMimic: Failed to register options: " + ex);
            }
        }

        private void PoleMimicGraphics_DrawSprites(On.PoleMimicGraphics.orig_DrawSprites orig, PoleMimicGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            if (sLeaser.sprites == null || sLeaser.sprites.Length == 0) return;
            if (Options == null) return;

            try
            {
                if (Options.UseCustomColors.Value)
                {
                    bool breathing = Options.Breathing.Value;
                    float breathFactor = 1f;
                    if (breathing)
                    {
                        breathFactor = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f * 0.5f + 0.5f; 
                    }

                    float poleMix = Options.PoleIntensity.Value / 100f;
                    if (poleMix > 0.01f)
                    {
                        Color original = sLeaser.sprites[0].color;
                        Color target = Options.PoleColor.Value;
                        sLeaser.sprites[0].color = Color.Lerp(original, target, poleMix * breathFactor);
                    }

                    float leafMix = Options.LeafIntensity.Value / 100f;
                    if (leafMix > 0.01f && sLeaser.sprites.Length > 1)
                    {
                        Color leafTarget = Options.LeafColor.Value;
                        for (int i = 1; i < sLeaser.sprites.Length; i++)
                        {
                            Color original = sLeaser.sprites[i].color;
                            sLeaser.sprites[i].color = Color.Lerp(original, leafTarget, leafMix * breathFactor);
                        }
                    }
                }
                else if (Options.UseSimpleHighlight.Value)
                {
                    Color originalColor = sLeaser.sprites[0].color;
                    Color targetColor = new Color(0.8f, 0.2f, 0.2f);
                    sLeaser.sprites[0].color = Color.Lerp(originalColor, targetColor, 0.3f);
                }
            }
            catch (System.Exception) {}
        }
    }

    public class RedMimicOptions : OptionInterface
    {
        public readonly Configurable<bool> UseSimpleHighlight;
        public readonly Configurable<bool> UseCustomColors;
        
        public readonly Configurable<Color> PoleColor;
        public readonly Configurable<int> PoleIntensity;
        public readonly Configurable<Color> LeafColor;
        public readonly Configurable<int> LeafIntensity;
        public readonly Configurable<bool> Breathing;
        
        private ManualLogSource _logger;

        public RedMimicOptions(BaseUnityPlugin plugin, ManualLogSource logger)
        {
            _logger = logger;
            _logger.LogInfo("RedMimicOptions: Constructor started.");
            
            UseSimpleHighlight = config.Bind("CfgUseSimpleHighlight", true, new ConfigurableInfo("Enable simple highlight (subtle tint)."));
            UseCustomColors = config.Bind("CfgUseCustomColors", false, new ConfigurableInfo("Enable advanced custom color configuration."));

            PoleColor = config.Bind("CfgPoleColor", new Color(1f, 0.2f, 0.2f), new ConfigurableInfo("Pole Target Color"));
            PoleIntensity = config.Bind("CfgPoleInt", 30, new ConfigurableInfo("Pole Intensity (%)", new ConfigAcceptableRange<int>(0, 100)));
            LeafColor = config.Bind("CfgLeafColor", new Color(1f, 0.2f, 0.6f), new ConfigurableInfo("Leaf Target Color"));
            LeafIntensity = config.Bind("CfgLeafInt", 90, new ConfigurableInfo("Leaf Intensity (%)", new ConfigAcceptableRange<int>(0, 100)));
            Breathing = config.Bind("CfgBreath", true, new ConfigurableInfo("Enable Breathing Effect"));
            
            _logger.LogInfo("RedMimicOptions: Constructor finished.");
        }

        public override void Initialize()
        {
            try 
            {
                _logger.LogInfo("RedMimicOptions: Initialize UI started.");
                base.Initialize();
                
                OpTab opTab = new OpTab(this, "Settings");
                this.Tabs = new OpTab[] { opTab };

                float leftX = 30f;
                float rightX = 320f;
                float topY = 550f;
                
                OpLabel title = new OpLabel(leftX, topY, "Red Mimic Configuration", true);
                
                float modeY = topY - 40f;
                OpCheckBox chkSimple = new OpCheckBox(UseSimpleHighlight, new Vector2(leftX, modeY));
                OpLabel lblSimple = new OpLabel(leftX + 30, modeY, "Simple Highlight (Subtle Red Tint)");
                
                float customModeY = modeY - 40f;
                OpCheckBox chkCustom = new OpCheckBox(UseCustomColors, new Vector2(leftX, customModeY));
                OpLabel lblCustom = new OpLabel(leftX + 30, customModeY, "Advanced Customization (Overrides Simple Highlight)");

                opTab.AddItems(title, chkSimple, lblSimple, chkCustom, lblCustom);

                float customY = customModeY - 40f;
                
                OpCheckBox chkBreath = new OpCheckBox(Breathing, new Vector2(leftX, customY));
                OpLabel lblBreath = new OpLabel(leftX + 30, customY, "Enable Breathing Effect");
                
                float poleY = customY - 60f;
                OpLabel lblPoleTitle = new OpLabel(leftX, poleY + 30, "Pole Settings");
                OpLabel lblPoleColor = new OpLabel(leftX, poleY, "Color:");
                OpColorPicker pkPole = new OpColorPicker(PoleColor, new Vector2(leftX, poleY - 160));
                OpLabel lblPoleInt = new OpLabel(leftX, poleY - 190, "Intensity:");
                OpSlider sldPole = new OpSlider(PoleIntensity, new Vector2(leftX, poleY - 220), 100);

                // Leaf
                float leafY = customY - 60f;
                OpLabel lblLeafTitle = new OpLabel(rightX, leafY + 30, "Leaf Settings");
                OpLabel lblLeafColor = new OpLabel(rightX, leafY, "Color:");
                OpColorPicker pkLeaf = new OpColorPicker(LeafColor, new Vector2(rightX, leafY - 160));
                OpLabel lblLeafInt = new OpLabel(rightX, leafY - 190, "Intensity:");
                OpSlider sldLeaf = new OpSlider(LeafIntensity, new Vector2(rightX, leafY - 220), 100);

                opTab.AddItems(
                    chkBreath, lblBreath,
                    lblPoleTitle, lblPoleColor, pkPole, lblPoleInt, sldPole,
                    lblLeafTitle, lblLeafColor, pkLeaf, lblLeafInt, sldLeaf
                );
                
                _logger.LogInfo("RedMimicOptions: UI Initialize finished.");
            }
            catch (System.Exception ex)
            {
                _logger.LogError("RedMimicOptions: UI Initialize Failed! " + ex);
            }
        }
    }
}
