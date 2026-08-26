// ==============================================================================
// 精卫填海 (JingWeiFillsSea) - Mod 入口
// ==============================================================================

using HarmonyLib;
using UnityEngine;
using Verse;

namespace JWFH
{
    /// <summary>
    /// Mod 入口类。
    /// </summary>
    public class JingWeiFillsSeaMod : Mod
    {
        public static ModSettings_JingWeiFillsSea Settings;

        public JingWeiFillsSeaMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ModSettings_JingWeiFillsSea>();

            var harmony = new Harmony("PooiWoop.JingWeiFillsSea");
            harmony.PatchAll();
            Log.Message("[精卫填海] Harmony 补丁已应用。");
        }

        /// <summary>
        /// 设置窗口标题（显示在 "选项 -> Mod 选项" 列表中）。
        /// </summary>
        public override string SettingsCategory()
        {
            return "精卫填海 (JingWeiFillsSea)";
        }

        /// <summary>
        /// 渲染 Mod 设置 UI 窗口。
        /// </summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Settings.DoWindowContents(inRect);
        }
    }
}
