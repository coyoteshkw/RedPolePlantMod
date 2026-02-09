using BepInEx;
using BepInEx.Logging;
using System.Security.Permissions;
using UnityEngine;
using Menu.Remix.MixedUI;
using RWCustom;
using System.Collections.Generic;

// --- 安全权限设置 ---
// 这两行代码允许插件访问游戏程序集中的私有成员（private members）。
// Rain World 的很多核心逻辑是私有的，没有这个权限我们将无法修改它们。
#pragma warning disable CS0618 // 忽略“类型或成员已过时”的警告
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace MoreVisiblePolePlants
{
    // --- BepInEx 插件元数据 ---
    // [BepInPlugin(GUID, Name, Version)]
    // GUID: 插件的唯一标识符，通常格式为 "作者名.插件名"。
    // Name: 插件在控制台显示的名称。
    // Version: 版本号。
    [BepInPlugin("coyoteshkw.MoreVisiblePolePlants", "More Visible Pole Plants", "0.1.6")]
    public class RedMimic : BaseUnityPlugin
    {
        // 静态引用配置选项实例，方便在任何地方访问配置数据
        public static RedMimicOptions Options;
        
        // 标记是否已经初始化过 Remix 菜单，防止重复注册
        private bool _initialized = false;

        // --- 生命周期：OnEnable ---
        // 类似于 Unity 的 Start() 或 Vue 的 mounted()。
        // 当 Mod 被加载时，BepInEx 会自动调用这个方法。
        public void OnEnable()
        {
            Logger.LogInfo("RedMimic: OnEnable called."); // 在 LogOutput.log 中打印日志

            // --- 注册钩子 (Hooks) ---
            // 语法：On.类名.方法名 += 我们的方法名;
            
            // 1. 监听 RainWorld.OnModsInit，用于注册配置菜单（Remix UI）
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            
            // 2. 监听 PoleMimicGraphics.DrawSprites，这是拟态草绘制画面的核心方法
            // 我们将在这里注入自定义的颜色逻辑
            On.PoleMimicGraphics.DrawSprites += PoleMimicGraphics_DrawSprites;
        }

        // --- Hook 方法：游戏初始化 ---
        // 当游戏加载完所有 Mod 并准备好时调用
        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            // 务必先调用 orig(self)，保证原版游戏和其他 Mod 的初始化逻辑正常执行
            try
            {
                orig(self);
            }
            catch (System.Exception ex)
            {
                // 捕获异常防止崩溃
                Logger.LogError("RedMimic: Error in orig(self) OnModsInit: " + ex);
            }

            // 防止重复运行初始化代码
            if (_initialized) return;
            _initialized = true;

            Logger.LogInfo("RedMimic: Initializing Options...");

            // --- 注册 Remix 配置菜单 ---
            try
            {
                // 实例化我们的配置类
                Options = new RedMimicOptions(this, Logger);
                // 告诉 Remix 系统："这个 Mod ID 对应的配置界面是 Options 这个实例"
                MachineConnector.SetRegisteredOI("coyoteshkw.MoreVisiblePolePlants", Options);
                Logger.LogInfo("RedMimic: Successfully registered options with MachineConnector.");
            }
            catch (System.Exception ex)
            {
                Logger.LogError("RedMimic: Failed to register options: " + ex);
            }
        }

        // --- Hook 方法：绘图逻辑 ---
        // 每一帧渲染拟态草时都会调用此方法
        // orig: 原版方法的引用
        // self: 当前这株拟态草的图形对象
        // sLeaser: SpriteLeaser，管理所有图像精灵（Sprite）的容器
        private void PoleMimicGraphics_DrawSprites(On.PoleMimicGraphics.orig_DrawSprites orig, PoleMimicGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            // 1. 执行原版绘图逻辑
            // 这会计算出拟态草的位置、形状以及“原本的伪装色”
            orig(self, sLeaser, rCam, timeStacker, camPos);

            // 安全检查：如果 Sprite 还没准备好，或者配置没加载，直接跳过
            if (sLeaser.sprites == null || sLeaser.sprites.Length == 0) return;
            if (Options == null) return;

            try
            {
                // --- 逻辑分支 A：高级自定义模式 ---
                if (Options.UseCustomColors.Value)
                {
                    // 1. 计算呼吸灯因子 (0.5 ~ 1.0 的循环波动)
                    bool breathing = Options.Breathing.Value;
                    float breathFactor = 1f;
                    if (breathing)
                    {
                        // Mathf.Sin 产生 -1 到 1 的波形
                        // Time.time * 3f 控制闪烁速度
                        breathFactor = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f * 0.5f + 0.5f; 
                    }

                    // 2. 处理杆子主体 (Sprite 索引 0)
                    float poleMix = Options.PoleIntensity.Value / 100f; // 将 0-100 转为 0.0-1.0
                    if (poleMix > 0.01f)
                    {
                        Color original = sLeaser.sprites[0].color;
                        Color target;
                        
                        // 检查是否开启 RGB 炫彩模式
                        if (Options.RainbowPole.Value)
                        {
                            // HSVToRGB: 色相(H), 饱和度(S), 亮度(V) 转 RGB
                            // Mathf.Repeat 让时间在 0-1 之间循环，实现彩虹流动
                            target = Color.HSVToRGB(Mathf.Repeat(Time.time * 1.5f, 1f), 1f, 1f);
                        }
                        else
                        {
                            target = Options.PoleColor.Value;
                        }

                        // Color.Lerp: 线性插值混合原色和目标色
                        // 最终混合比例 = 用户设定的强度 * 呼吸因子
                        sLeaser.sprites[0].color = Color.Lerp(original, target, poleMix * breathFactor);
                    }

                    // 3. 处理叶子/倒刺 (Sprite 索引 1 及以后)
                    float leafMix = Options.LeafIntensity.Value / 100f;
                    if (leafMix > 0.01f && sLeaser.sprites.Length > 1)
                    {
                        Color leafTarget;

                        if (Options.RainbowLeaves.Value)
                        {
                            // 这里的 +0.4f 是为了让叶子的颜色循环和杆子错开，避免颜色重叠
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
                // --- 逻辑分支 B：简单高亮模式 ---
                else if (Options.UseSimpleHighlight.Value)
                {
                    Color originalColor = sLeaser.sprites[0].color;
                    Color targetColor = new Color(0.8f, 0.2f, 0.2f); // 固定淡红色
                    // 简单混合 30%
                    sLeaser.sprites[0].color = Color.Lerp(originalColor, targetColor, 0.3f);
                }
            }
            catch (System.Exception) 
            {
                // 绘图循环极其频繁（每秒60次以上），这里发生错误通常忽略，避免日志刷屏导致游戏卡顿
            }
        }
    }

    // --- 配置界面类 ---
    // 继承 OptionInterface 以接入 Rain World Remix 系统
    public class RedMimicOptions : OptionInterface
    {
        // --- 配置项定义 (Configurables) ---
        // Configurable<T> 会自动负责读写保存文件
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
        
        // --- 本地化字典 (中文翻译) ---
        private static readonly Dictionary<string, string> _cn = new Dictionary<string, string>()
        {
            // 界面文本
            { "Pole Plants Configuration", "拟态草高亮设置" },
            { "Simple Highlight (Subtle Red Tint)", "简单高亮 (淡红色滤镜)" },
            { "Advanced Customization (Overrides Simple Highlight)", "高级自定义 (覆盖简单高亮)" },
            { "Enable Breathing Effect", "开启呼吸灯特效" },
            { "Pole Settings", "杆子设置" },
            { "Leaf Settings", "叶子设置" },
            { "RGB Rainbow Mode", "RGB 炫彩模式" },
            { "Color (Ignored if Rainbow):", "颜色 (炫彩模式下忽略):" },
            { "Intensity:", "混合强度:" },
            
            // 配置项描述 (Tooltip)
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

        // --- 辅助方法：翻译 ---
        private string GetText(string original)
        {
            // 如果当前游戏语言是中文，尝试查表
            if (RWCustom.Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese)
            {
                if (_cn.TryGetValue(original, out string res)) return res;
            }
            // 否则返回英文原文
            return original;
        }

        // --- 构造函数 ---
        // 在这里绑定配置项的键名、默认值和描述
        public RedMimicOptions(BaseUnityPlugin plugin, ManualLogSource logger)
        {
            _logger = logger;
            // config.Bind("Key", DefaultValue, Info)
            // 这里的 GetText 用于翻译鼠标悬停时显示的 Tooltip
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

        // --- 初始化 UI ---
        // 当玩家点击 Remix 菜单中的 Config 按钮时调用
        public override void Initialize()
        {
            try 
            {
                base.Initialize();
                // 创建一个标签页
                OpTab opTab = new OpTab(this, "Settings");
                this.Tabs = new OpTab[] { opTab };

                // --- UI 布局常量 ---
                // 使用变量来控制位置，方便整体调整
                float margin = 20f;
                float topY = 560f;
                float modeY = 520f;
                float customModeY = 490f;
                float breathingY = 450f;
                
                // 背景矩形区域参数
                float rectTop = 410f;
                float rectHeight = 360f;
                float colWidth = 270f;
                float colGap = 20f;
                
                float col1X = margin;
                float col2X = margin + colWidth + colGap;

                // --- 1. 顶部区域：标题与模式 ---
                OpLabel title = new OpLabel(margin, topY, GetText("Pole Plants Configuration"), true); // true 表示大字体

                OpCheckBox chkSimple = new OpCheckBox(UseSimpleHighlight, new Vector2(margin, modeY));
                OpLabel lblSimple = new OpLabel(margin + 30f, modeY, GetText("Simple Highlight (Subtle Red Tint)"));
                
                // 增加间距以区分不同区域
                OpCheckBox chkCustom = new OpCheckBox(UseCustomColors, new Vector2(margin, customModeY));
                OpLabel lblCustom = new OpLabel(margin + 30f, customModeY, GetText("Advanced Customization (Overrides Simple Highlight)"));
                
                // --- 2. 中部区域：全局特效 ---
                OpCheckBox chkBreath = new OpCheckBox(Breathing, new Vector2(margin, breathingY));
                OpLabel lblBreath = new OpLabel(margin + 30f, breathingY, GetText("Enable Breathing Effect"));

                // --- 3. 底部区域：双列设置 ---
                // 绘制两个半透明背景框 (OpRect)
                // OpRect(Pos, Size, Alpha)
                OpRect rectPole = new OpRect(new Vector2(col1X, rectTop - rectHeight), new Vector2(colWidth, rectHeight), 0.3f); 
                OpRect rectLeaf = new OpRect(new Vector2(col2X, rectTop - rectHeight), new Vector2(colWidth, rectHeight), 0.3f);
                
                float innerTop = rectTop - 10f; // 框内内容的起始 Y
                
                // === 左列：杆子设置 (Pole Settings) ===
                OpLabel lblPoleTitle = new OpLabel(col1X + 10f, innerTop - 30f, GetText("Pole Settings"), true); 
                
                OpCheckBox chkRainbowPole = new OpCheckBox(RainbowPole, new Vector2(col1X + 10f, innerTop - 70f));
                OpLabel lblRainbowPole = new OpLabel(col1X + 40f, innerTop - 70f, GetText("RGB Rainbow Mode"));

                OpLabel lblPoleColor = new OpLabel(col1X + 10f, innerTop - 110f, GetText("Color (Ignored if Rainbow):"));
                OpColorPicker pkPole = new OpColorPicker(PoleColor, new Vector2(col1X + 10f, innerTop - 270f)); 
                
                OpLabel lblPoleInt = new OpLabel(col1X + 10f, innerTop - 300f, GetText("Intensity:"));
                // Slider 宽度设为 colWidth - 30，确保在框内居中
                OpSlider sldPole = new OpSlider(PoleIntensity, new Vector2(col1X + 10f, innerTop - 330f), (int)(colWidth - 30f));

                // === 右列：叶子设置 (Leaf Settings) ===
                OpLabel lblLeafTitle = new OpLabel(col2X + 10f, innerTop - 30f, GetText("Leaf Settings"), true); 

                OpCheckBox chkRainbowLeaf = new OpCheckBox(RainbowLeaves, new Vector2(col2X + 10f, innerTop - 70f));
                OpLabel lblRainbowLeaf = new OpLabel(col2X + 40f, innerTop - 70f, GetText("RGB Rainbow Mode"));

                OpLabel lblLeafColor = new OpLabel(col2X + 10f, innerTop - 110f, GetText("Color (Ignored if Rainbow):"));
                OpColorPicker pkLeaf = new OpColorPicker(LeafColor, new Vector2(col2X + 10f, innerTop - 270f));
                
                OpLabel lblLeafInt = new OpLabel(col2X + 10f, innerTop - 300f, GetText("Intensity:"));
                OpSlider sldLeaf = new OpSlider(LeafIntensity, new Vector2(col2X + 10f, innerTop - 330f), (int)(colWidth - 30f));

                // --- 4. 将所有元素添加到画布 ---
                opTab.AddItems(
                    title,
                    chkSimple, lblSimple,
                    chkCustom, lblCustom,
                    chkBreath, lblBreath,
                    rectPole, rectLeaf, // 先添加背景框，使其位于底层
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
