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

namespace MoreVisiblePolePlants
{
    [BepInPlugin("coyoteshkw.MoreVisiblePolePlants", "More Visible Pole Plants", "0.1.6")]
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
                MachineConnector.SetRegisteredOI("coyoteshkw.MoreVisiblePolePlants", Options);
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
                        
                        if (Options.RainbowPole.Value)
                        {
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
        public readonly Configurable<bool> RainbowPole;
        public readonly Configurable<bool> RainbowLeaves;
        public readonly Configurable<Color> PoleColor;
        public readonly Configurable<int> PoleIntensity;
        public readonly Configurable<Color> LeafColor;
        public readonly Configurable<int> LeafIntensity;
        public readonly Configurable<bool> Breathing;
        
        private ManualLogSource _logger;
        
        // Localization Dictionary
        private static readonly Dictionary<string, string> _cn = new Dictionary<string, string>()
        {
            { "Pole Plants Configuration", "拟态草高亮设置" },
            { "Simple Highlight (Subtle Red Tint)", "简单高亮 (淡红色滤镜)" },
            { "Advanced Customization (Overrides Simple Highlight)", "高级自定义 (覆盖简单高亮)" },
            { "Enable Breathing Effect", "开启呼吸灯特效" },
            { "Pole Settings", "杆子设置" },
            { "Leaf Settings", "叶子设置" },
            { "RGB Rainbow Mode", "RGB 炫彩模式" },
            { "Color (Ignored if Rainbow):", "颜色 (炫彩模式下忽略):" },
            { "Intensity:", "混合强度:" },
            
            // Descriptions
            { "Enable simple highlight (subtle tint).", "开启简单高亮模式（淡红色）。" },
            { "Enable advanced custom color configuration.", "开启高级自定义颜色配置。" },
            { "Cycle through RGB colors for the pole.", "杆子颜色在 RGB 光谱中循环。" },
            { "Cycle through RGB colors for the leaves.", "叶子颜色在 RGB 光谱中循环。" },
            { "Pole Target Color", "杆子目标颜色" },
            { "Pole Intensity (%)", "杆子变色程度 (%)" },
            { "Leaf Target Color", "叶子目标颜色" },
            { "Leaf Intensity (%)", "叶子变色程度 (%)" },
            { "Enable Breathing Effect Description", "开启呼吸灯闪烁效果。" }
        };

        private string GetText(string original)
        {
            if (RWCustom.Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese)
            {
                if (_cn.TryGetValue(original, out string res)) return res;
            }
            return original;
        }

        public RedMimicOptions(BaseUnityPlugin plugin, ManualLogSource logger)
        {
            _logger = logger;
            UseSimpleHighlight = config.Bind("CfgUseSimpleHighlight", true, new ConfigurableInfo(GetText("Enable simple highlight (subtle tint).")));
            UseCustomColors = config.Bind("CfgUseCustomColors", false, new ConfigurableInfo(GetText("Enable advanced custom color configuration.")));
            RainbowPole = config.Bind("CfgRainbowPole", false, new ConfigurableInfo(GetText("Cycle through RGB colors for the pole.")));
            RainbowLeaves = config.Bind("CfgRainbowLeaves", false, new ConfigurableInfo(GetText("Cycle through RGB colors for the leaves.")));
            PoleColor = config.Bind("CfgPoleColor", new Color(1f, 0.2f, 0.2f), new ConfigurableInfo(GetText("Pole Target Color")));
            PoleIntensity = config.Bind("CfgPoleInt", 30, new ConfigurableInfo(GetText("Pole Intensity (%)"), new ConfigAcceptableRange<int>(0, 100)));
            LeafColor = config.Bind("CfgLeafColor", new Color(1f, 0.2f, 0.6f), new ConfigurableInfo(GetText("Leaf Target Color")));
            LeafIntensity = config.Bind("CfgLeafInt", 90, new ConfigurableInfo(GetText("Leaf Intensity (%)"), new ConfigAcceptableRange<int>(0, 100)));
            Breathing = config.Bind("CfgBreath", false, new ConfigurableInfo(GetText("Enable Breathing Effect")));
        }

        public override void Initialize()
        {
            try 
            {
                base.Initialize();
                OpTab opTab = new OpTab(this, "Settings");
                this.Tabs = new OpTab[] { opTab };

                // --- Layout Constants ---
                float margin = 20f;
                float topY = 560f;
                float modeY = 520f;
                float customModeY = 490f;
                float breathingY = 450f;
                
                // Rect Area
                float rectTop = 410f;
                float rectHeight = 360f;
                float colWidth = 270f;
                float colGap = 20f;
                
                float col1X = margin;
                float col2X = margin + colWidth + colGap;

                // --- Header ---
                OpLabel title = new OpLabel(margin, topY, GetText("Pole Plants Configuration"), true);

                // --- Modes ---
                OpCheckBox chkSimple = new OpCheckBox(UseSimpleHighlight, new Vector2(margin, modeY));
                OpLabel lblSimple = new OpLabel(margin + 30f, modeY, GetText("Simple Highlight (Subtle Red Tint)"));
                
                OpCheckBox chkCustom = new OpCheckBox(UseCustomColors, new Vector2(margin, customModeY));
                OpLabel lblCustom = new OpLabel(margin + 30f, customModeY, GetText("Advanced Customization (Overrides Simple Highlight)"));
                
                // --- Breathing ---
                OpCheckBox chkBreath = new OpCheckBox(Breathing, new Vector2(margin, breathingY));
                OpLabel lblBreath = new OpLabel(margin + 30f, breathingY, GetText("Enable Breathing Effect"));

                // --- Background Rects ---
                // Alpha 0.2 for subtle background
                OpRect rectPole = new OpRect(new Vector2(col1X, rectTop - rectHeight), new Vector2(colWidth, rectHeight), 0.3f); 
                OpRect rectLeaf = new OpRect(new Vector2(col2X, rectTop - rectHeight), new Vector2(colWidth, rectHeight), 0.3f);
                
                // --- Column 1: Pole Settings ---
                // Y offsets relative to rectTop
                float innerTop = rectTop - 10f; 
                
                OpLabel lblPoleTitle = new OpLabel(col1X + 10f, innerTop - 30f, GetText("Pole Settings"), true); // Big Text
                
                OpCheckBox chkRainbowPole = new OpCheckBox(RainbowPole, new Vector2(col1X + 10f, innerTop - 70f));
                OpLabel lblRainbowPole = new OpLabel(col1X + 40f, innerTop - 70f, GetText("RGB Rainbow Mode"));

                OpLabel lblPoleColor = new OpLabel(col1X + 10f, innerTop - 110f, GetText("Color (Ignored if Rainbow):"));
                OpColorPicker pkPole = new OpColorPicker(PoleColor, new Vector2(col1X + 10f, innerTop - 270f)); // 150 height
                
                OpLabel lblPoleInt = new OpLabel(col1X + 10f, innerTop - 300f, GetText("Intensity:"));
                OpSlider sldPole = new OpSlider(PoleIntensity, new Vector2(col1X + 10f, innerTop - 330f), (int)(colWidth - 30f)); // Fit width

                // --- Column 2: Leaf Settings ---
                OpLabel lblLeafTitle = new OpLabel(col2X + 10f, innerTop - 30f, GetText("Leaf Settings"), true); // Big Text

                OpCheckBox chkRainbowLeaf = new OpCheckBox(RainbowLeaves, new Vector2(col2X + 10f, innerTop - 70f));
                OpLabel lblRainbowLeaf = new OpLabel(col2X + 40f, innerTop - 70f, GetText("RGB Rainbow Mode"));

                OpLabel lblLeafColor = new OpLabel(col2X + 10f, innerTop - 110f, GetText("Color (Ignored if Rainbow):"));
                OpColorPicker pkLeaf = new OpColorPicker(LeafColor, new Vector2(col2X + 10f, innerTop - 270f));
                
                OpLabel lblLeafInt = new OpLabel(col2X + 10f, innerTop - 300f, GetText("Intensity:"));
                OpSlider sldLeaf = new OpSlider(LeafIntensity, new Vector2(col2X + 10f, innerTop - 330f), (int)(colWidth - 30f));

                // Add Items
                opTab.AddItems(
                    title,
                    chkSimple, lblSimple,
                    chkCustom, lblCustom,
                    chkBreath, lblBreath,
                    rectPole, rectLeaf,
                    lblPoleTitle, chkRainbowPole, lblRainbowPole, lblPoleColor, pkPole, lblPoleInt, sldPole,
                    lblLeafTitle, chkRainbowLeaf, lblRainbowLeaf, lblLeafColor, pkLeaf, lblLeafInt, sldLeaf
                );
            }
            catch (System.Exception ex)
            {
                _logger.LogError("RedMimicOptions: UI Initialize Failed! " + ex);
            }
        }
    }
}