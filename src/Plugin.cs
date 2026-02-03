using BepInEx;
using BepInEx.Logging;
using System.Security.Permissions;
using UnityEngine;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace RedPolePlantMod
{
    [BepInPlugin("com.coyoteshkw.RedPolePlantMod", "RedPolePlantMod", "0.1.0")]
    public class RedMimic : BaseUnityPlugin
    {
        // OnEnable 相当于 Vue 的 mounted()，在 Mod 加载完成时执行
        public void OnEnable()
        {
            // 监听日志，确定 Mod 加载成功
            Logger.LogInfo("拟态草高亮 Mod 已加载！");

            // 注册钩子 (Hook)
            // 意思：当游戏要运行 PoleMimicGraphics 类的 DrawSprites 方法时，先问问我
            On.PoleMimicGraphics.DrawSprites += PoleMimicGraphics_DrawSprites;
        }

        // 这是我们的自定义逻辑
        // orig: 原始的方法（你可以选择调用它，或者完全屏蔽它）
        // self: 当前这根拟态草的实例（相当于 this）
        // sLeaser: 负责管理图像精灵的容器
        private void PoleMimicGraphics_DrawSprites(On.PoleMimicGraphics.orig_DrawSprites orig, PoleMimicGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            // 1. 先让原版游戏把草画出来（计算位置、形状、原本的颜色）
            orig(self, sLeaser, rCam, timeStacker, camPos);

            // 2. 修改颜色
            // 拟态草主要由 sprites[0] 构成杆子主体
            if (sLeaser.sprites != null && sLeaser.sprites.Length > 0)
            {
                // 将颜色强制设为红色
                // sLeaser.sprites[0].color = Color.red;

                // 进阶：如果你想让它在黑暗中发光（无视光照），取消下面这行的注释
                // sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Basic"];

				// == 方案1: 降低红色颜色
				// 获取游戏原本计算出的伪装色
				// Color originalColor = sLeaser.sprites[0].color;
				
				// 并不是变成纯红，而是保留 60% 原色，混合 40% 的暗红色
				// Color.Lerp(颜色A, 颜色B, B的比例 0.0-1.0)
				// sLeaser.sprites[1].color = Color.Lerp(originalColor, new Color(0.8f, 0.2f, 0.2f), 0.4f);

				// == 方案2: 呼吸灯效果
				// 计算呼吸因子：产生一个在 0.0 到 0.5 之间循环的数值
				// Time.time * 3f 控制速度（数字越大呼吸越快）
				// float pulse = (Mathf.Sin(Time.time * 3f) + 1f) / 4f; 

				// 让它在“原色”和“亮橙色”之间循环
				// sLeaser.sprites[0].color = Color.Lerp(sLeaser.sprites[0].color, new Color(1f, 0.5f, 0f), pulse);

				// == 方案3: 让叶子变红
				// 遍历所有 sprite
				for (int i = 0; i < sLeaser.sprites.Length; i++)
				{
					// i == 0 是杆子本体，我们决定不改它，让它完美伪装
					if (i == 0) continue;

					// i > 0 的通常是杆子上的叶子/倒刺装饰
					// 把叶子改成鲜艳的“警告色”，比如亮紫色或深红
					sLeaser.sprites[i].color = new Color(1f, 0.2f, 0.4f); 
					
					// 可选：让叶子不受光照影响，像霓虹灯一样亮
					// sLeaser.sprites[i].shader = rCam.game.rainWorld.Shaders["Basic"];
				}
            }
        }
    }
}
