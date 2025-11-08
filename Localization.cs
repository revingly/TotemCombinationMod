using System;
using System.Collections.Generic;
using UnityEngine;
using SodaCraft.Localizations;

namespace TotemCombination
{
    public static class LocalizationHelper
    {
        // Localization keys
        public const string SameTotemCannotCombine = "TotemCombination_SameTotem_CannotCombine";
        public const string TotemCannotUpgrade = "TotemCombination_Totem_CannotUpgrade";
        public const string TotemUpgraded = "TotemCombination_Totem_Upgraded";

        private static readonly Dictionary<SystemLanguage, Dictionary<string, string>> translations = new Dictionary<SystemLanguage, Dictionary<string, string>>
        {
            // English (default/fallback)
            {
                SystemLanguage.English, new Dictionary<string, string>
                {
                    { SameTotemCannotCombine, "Cannot combine the same item."},
                    { TotemCannotUpgrade, "Totem cannot be upgraded further." },
                    { TotemUpgraded, "Upgraded Totem -> {0}" }
                }
            },

            // Chinese Simplified (简体中文)
            {
                SystemLanguage.ChineseSimplified, new Dictionary<string, string>
                {
                    { SameTotemCannotCombine, "无法组合相同的物品。"},
                    { TotemCannotUpgrade, "图腾无法进一步升级。" },
                    { TotemUpgraded, "图腾已升级 -> {0}" }
                }
            },

            // Japanese (日本語)
            {
                SystemLanguage.Japanese, new Dictionary<string, string>
                {
                    { SameTotemCannotCombine, "同じアイテムを組み合わせることはできません。"},
                    { TotemCannotUpgrade, "トーテムはこれ以上アップグレードできません。" },
                    { TotemUpgraded, "トーテムをアップグレード -> {0}" }
                }
            },

            // French (Français)
            {
                SystemLanguage.French, new Dictionary<string, string>
                {
                    { SameTotemCannotCombine, "Impossible de combiner le même objet." },
                    { TotemCannotUpgrade, "Le totem ne peut pas être amélioré davantage." },
                    { TotemUpgraded, "Totem amélioré -> {0}" }
                }
            }
        };

        public static string Get(string key, params object[] args)
        {
            try
            {
                SystemLanguage currentLanguage = LocalizationManager.CurrentLanguage;

                // Try to get translation for current language
                if (translations.TryGetValue(currentLanguage, out var languageDict) &&
                    languageDict.TryGetValue(key, out var translation))
                {
                    return args.Length > 0 ? string.Format(translation, args) : translation;
                }

                // Fallback to English
                if (translations.TryGetValue(SystemLanguage.English, out var englishDict) &&
                    englishDict.TryGetValue(key, out var englishTranslation))
                {
                    return args.Length > 0 ? string.Format(englishTranslation, args) : englishTranslation;
                }

                // If key not found, return the key itself as a fallback
                Debug.LogWarning($"[TotemCombination] Missing translation for key: {key}");
                return key;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TotemCombination] Localization error for key '{key}': {ex}");
                return key;
            }
        }
    }
}

