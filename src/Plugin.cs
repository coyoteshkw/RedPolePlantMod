using BepInEx;
using BepInEx.Logging;
using System.Security.Permissions;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace RedPolePlantMod;
[BepInPlugin("com.coyoteshkw.RedPolePlantMod", "RedPolePlantMod", "0.1.0")]
{
    // BepInEx 插件标识
    // [BepInPlugin("com.yourname.redmimic", "Red Pole Mimic", "1.0.0")]
	public class RedMimic : BaseUnityPlugin
    {
        // OnEnable 相当于 Vue 的 mounted()，在 Mod 加载完成时执行
        public void OnEnable()
        {
            // 监听日志，确定 Mod 加载成功
            Logger.LogInfo("拟态草高亮 Mod 已加载！");

            // 注册钩子 (Hook)
            // 意思：当游戏要运行 PoleMimic 类的 DrawSprites 方法时，先问问我
            On.PoleMimic.DrawSprites += PoleMimic_DrawSprites;
        }

        // 这是我们的自定义逻辑
        // orig: 原始的方法（你可以选择调用它，或者完全屏蔽它）
        // self: 当前这根拟态草的实例（相当于 this）
        // sLeaser: 负责管理图像精灵的容器
        private void PoleMimic_DrawSprites(On.PoleMimic.orig_DrawSprites orig, PoleMimic self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            // 1. 先让原版游戏把草画出来（计算位置、形状、原本的颜色）
            orig(self, sLeaser, rCam, timeStacker, camPos);

            // 2. 修改颜色
            // 拟态草主要由 sprites[0] 构成杆子主体
            if (sLeaser.sprites != null && sLeaser.sprites.Length > 0)
            {
                // 将颜色强制设为红色
                sLeaser.sprites[0].color = Color.red;

                // 进阶：如果你想让它在黑暗中发光（无视光照），取消下面这行的注释
                // sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Basic"];
            }
        }
    }
}
