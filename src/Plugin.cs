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
    [BepInPlugin("com.coyoteshkw.RedPolePlantMod", "RedPolePlantMod", "0.1.2")]
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

                    // --- Pole Logic ---
                    float poleMix = Options.PoleIntensity.Value / 100f;
                    if (poleMix > 0.01f)
                    {
                        Color original = sLeaser.sprites[0].color;
                        Color target;
                        
                        // 如果启用了炫彩模式，覆盖目标颜色
                        if (Options.RainbowPole.Value)
                        {
                            // HSV 循环：色相(Hue) 随时间变化
                            target = Color.HSVToRGB(Mathf.Repeat(Time.time * 1.5f, 1f), 1f, 1f);
                        }
                        else
                        {
                            target = Options.PoleColor.Value;
                        }

                        sLeaser.sprites[0].color = Color.Lerp(original, target, poleMix * breathFactor);
                    }

                    // --- Leaf Logic ---
                    float leafMix = Options.LeafIntensity.Value / 100f;
                    if (leafMix > 0.01f && sLeaser.sprites.Length > 1)
                    {
                        Color leafTarget;

                        if (Options.RainbowLeaves.Value)
                        {
                            // HSV 循环：色相(Hue) 随时间变化，且增加偏移量 (0.4f)，避免与杆子颜色同步
                            leafTarget = Color.HSVToRGB(Mathf.Repeat(Time.time * 1.5f + 0.4f, 1f), 1f, 1f);
                        }
                        else
                        {
                            leafTarget = Options.LeafColor.Value;
                        }

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
        
        // Rainbow Modes
        public readonly Configurable<bool> RainbowPole;
        public readonly Configurable<bool> RainbowLeaves;

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

            RainbowPole = config.Bind("CfgRainbowPole", false, new ConfigurableInfo("Cycle through RGB colors for the pole."));
            RainbowLeaves = config.Bind("CfgRainbowLeaves", false, new ConfigurableInfo("Cycle through RGB colors for the leaves."));

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
                
                // Pole Section
                float poleY = customY - 60f;
                OpLabel lblPoleTitle = new OpLabel(leftX, poleY + 30, "Pole Settings");
                
                // Rainbow Toggle for Pole
                OpCheckBox chkRainbowPole = new OpCheckBox(RainbowPole, new Vector2(leftX, poleY));
                OpLabel lblRainbowPole = new OpLabel(leftX + 30, poleY, "RGB Rainbow Mode");

                OpLabel lblPoleColor = new OpLabel(leftX, poleY - 30, "Color (Ignored if Rainbow):");
                OpColorPicker pkPole = new OpColorPicker(PoleColor, new Vector2(leftX, poleY - 190));
                OpLabel lblPoleInt = new OpLabel(leftX, poleY - 220, "Intensity:");
                OpSlider sldPole = new OpSlider(PoleIntensity, new Vector2(leftX, poleY - 250), 100);

                // Leaf Section
                float leafY = customY - 60f;
                OpLabel lblLeafTitle = new OpLabel(rightX, leafY + 30, "Leaf Settings");

                // Rainbow Toggle for Leaf
                OpCheckBox chkRainbowLeaf = new OpCheckBox(RainbowLeaves, new Vector2(rightX, leafY));
                OpLabel lblRainbowLeaf = new OpLabel(rightX + 30, leafY, "RGB Rainbow Mode");

                OpLabel lblLeafColor = new OpLabel(rightX, leafY - 30, "Color (Ignored if Rainbow):");
                OpColorPicker pkLeaf = new OpColorPicker(LeafColor, new Vector2(rightX, leafY - 190));
                OpLabel lblLeafInt = new OpLabel(rightX, leafY - 220, "Intensity:");
                OpSlider sldLeaf = new OpSlider(LeafIntensity, new Vector2(rightX, leafY - 250), 100);

                opTab.AddItems(
                    chkBreath, lblBreath,
                    lblPoleTitle, chkRainbowPole, lblRainbowPole, lblPoleColor, pkPole, lblPoleInt, sldPole,
                    lblLeafTitle, chkRainbowLeaf, lblRainbowLeaf, lblLeafColor, pkLeaf, lblLeafInt, sldLeaf
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