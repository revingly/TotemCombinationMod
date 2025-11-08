using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Duckov.UI;
using HarmonyLib;
using ItemStatsSystem;
using SodaCraft.Localizations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TotemCombination
{
    [HarmonyPatch(typeof(InventoryEntry))]
    public class CombineTotemsPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(InventoryEntry.OnDrop))]
        public static bool Prefix(PointerEventData eventData, InventoryEntry __instance)
        {
            Debug.Log("[TotemCombination] InventoryEntry OnDrop Prefix called.");
            Debug.Log($"[TotemCombination] InventoryEntry OnDrop eventData.used: {eventData.used}, __instance.Editable: {__instance.Editable}, eventData.button: {eventData.button}");

            if (eventData.used)
            {
                return false;
            }
            if (!__instance.Editable)
            {
                return false;
            }
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return false;
            }
            IItemDragSource component = eventData.pointerDrag.gameObject.GetComponent<IItemDragSource>();
            if (component == null)
            {
                return false;
            }
            if (!component.IsEditable())
            {
                return false;
            }
            Item item = component.GetItem();
            if (item == null)
            {
                return false;
            }
            if (item.Sticky && !__instance.Master.Target.AcceptSticky)
            {
                return false;
            }

            if (Keyboard.current != null && Keyboard.current.ctrlKey.isPressed)
            {
                if (__instance.Content != null)
                {
                    NotificationText.Push("UI_Inventory_TargetOccupiedCannotSplit".ToPlainText());
                    return false;
                }
                Debug.Log("SPLIT");
                SplitDialogue.SetupAndShow(item, __instance.Master.Target, __instance.Index);
                return false;
            }
            else
            {
                ItemUIUtilities.NotifyPutItem(item, false);
                if (__instance.Content == null)
                {
                    item.Detach();
                    __instance.Master.Target.AddAt(item, __instance.Index);
                    return false;
                }
                if (__instance.Content.TypeID == item.TypeID && __instance.Content.Stackable)
                {
                    __instance.Content.Combine(item);
                    return false;
                }

                if (__instance.Content.TypeID == item.TypeID && isTotem(__instance, item))
                {
                    if (__instance.Index == item.InInventory.GetIndex(item))
                    {
                        Debug.Log("[TotemCombination] Dropped item is the same as the target slot item. No action taken.");
                        NotificationText.Push(LocalizationHelper.Get(LocalizationHelper.SameTotemCannotCombine));
                        return false;
                    }

                    Debug.Log($"[TotemCombination] Both items are totems and same type and same level. Combining together");
                    Debug.Log($"[TotemCombination] Item 1: {__instance.Content.DisplayName}, Item 2: {item.DisplayName}");
                    if (!TryGetUpgradeTypeId(item, out int upgradeTypeId))
                    {
                        Debug.LogWarning($"[TotemCombination] No upgrade mapping found for totem TypeID {item.TypeID}. Aborting combination.");
                        NotificationText.Push(LocalizationHelper.Get(LocalizationHelper.TotemCannotUpgrade));
                    }
                    else
                    {
                        StartTotemCombination(__instance, item, upgradeTypeId);
                    }
                    return false;
                }
                Inventory inInventory = item.InInventory;
                Inventory target = __instance.Master.Target;
                if (inInventory != null)
                {
                    int num = inInventory.GetIndex(item);
                    int num2 = __instance.Index;
                    Item content = __instance.Content;
                    if (content != item)
                    {
                        item.Detach();
                        content.Detach();
                        inInventory.AddAt(content, num);
                        target.AddAt(item, num2);
                    }
                }
                return false;
            }
        }

        private static void StartTotemCombination(InventoryEntry entry, Item incomingItem, int upgradeTypeId)
        {
            Item baseTotem = entry.Content;
            if (baseTotem == null)
            {
                Debug.LogWarning("[TotemCombination] Target slot emptied before combination started.");
                return;
            }

            Inventory targetInventory = entry.Master.Target;

            UniTask.Void(async () =>
            {
                try
                {
                    Item upgradedTotem = await ItemAssetsCollection.InstantiateAsync(upgradeTypeId);
                    if (upgradedTotem == null)
                    {
                        Debug.LogError($"[TotemCombination] Failed to instantiate upgraded totem for TypeID {upgradeTypeId}.");
                        return;
                    }
                    Debug.Log($"[TotemCombination] Created upgraded totem with TypeID: {upgradedTotem.TypeID} - instance ID: {upgradedTotem.GetInstanceID()}");

                    // Remove the consumed totems from the world/inventory.
                    if (baseTotem != null)
                    {
                        baseTotem.Detach();
                        baseTotem.DestroyTree();
                    }

                    if (incomingItem != null)
                    {
                        incomingItem.Detach();
                        incomingItem.DestroyTree();
                    }

                    try
                    {
                        targetInventory.AddItem(upgradedTotem);
                    }
                    catch (Exception addEx)
                    {
                        Debug.LogWarning($"[TotemCombination] insert failed: {addEx.Message}.");
                        return;
                    }

                    ItemUIUtilities.NotifyPutItem(upgradedTotem, false);
                    NotificationText.Push(LocalizationHelper.Get(LocalizationHelper.TotemUpgraded, upgradedTotem.DisplayName));
                    Debug.Log($"[TotemCombination] Totem upgraded to {upgradedTotem.DisplayName} (TypeID: {upgradeTypeId}).");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TotemCombination] Exception during totem combination: {ex}");
                }
            });
        }

        private static bool TryGetUpgradeTypeId(Item item, out int upgradeTypeId)
        {
            upgradeTypeId = 0;
            if (item == null)
            {
                return false;
            }

            if (TotemUpgradeLookup.TryGetValue(item.TypeID, out upgradeTypeId))
            {
                return true;
            }

            return false;
        }

        private static readonly Dictionary<int, int> TotemUpgradeLookup = new Dictionary<int, int>
        {
            { 319, 318 }, // Aegis I → Aegis II
            { 318, 947 }, // Aegis II → Aegis III

            { 321, 320 }, // Assault I → Assault II
            { 320, 957 }, // Assault II → Assault III

            { 323, 322 }, // Warrior I → Warrior II
            { 322, 985 }, // Warrior II → Warrior III

            { 993, 324 }, // Agile I → Agile II
            { 324, 992 }, // Agile II → Agile III

            { 995, 994 }, // Sturdy I → Sturdy II
            { 994, 325 }, // Sturdy II → Sturdy III

            { 369, 965 }, // Physical RES I → Physical RES II
            { 965, 966 }, // Physical RES II → Physical RES III

            { 370, 952 }, // Marathon I → Marathon II
            { 952, 953 }, // Marathon II → Marathon III

            { 431, 430 }, // Electric RES I → Electric RES II
            { 430, 951 }, // Electric RES II → Electric RES III

            { 975, 432 }, // Efficiency I → Efficiency II
            { 432, 974 }, // Efficiency II → Efficiency III

            { 978, 977 }, // Gun Control I → Gun Control II
            { 977, 976 }, // Gun Control II → Gun Control III

            { 436, 435 }, // Ninja I → Ninja II
            { 435, 964 }, // Ninja II → Ninja III

            { 956, 954 }, // Fire RES I → Fire RES II
            { 954, 955 }, // Fire RES II → Fire RES III

            { 960, 958 }, // Recovery I → Recovery II
            { 958, 959 }, // Recovery II → Recovery III

            { 963, 961 }, // HP I → HP II
            { 961, 962 }, // HP II → HP III

            { 969, 967 }, // Poison RES I → Poison RES II
            { 967, 968 }, // Poison RES II → Poison RES III

            { 972, 970 }, // Space RES I → Space RES II
            { 970, 971 }, // Space RES II → Space RES III

            { 981, 979 }, // Headshot I → Headshot II
            { 979, 980 }, // Headshot II → Headshot III

            { 984, 982 }, // Sniper I → Sniper II
            { 982, 983 }, // Sniper II → Sniper III

            { 988, 986 }, // Berserk I → Berserk II
            { 986, 987 }, // Berserk II → Berserk III

            { 991, 989 }, // Perception I → Perception II
            { 989, 990 }, // Perception II → Perception III
        };

        private static bool isTotem(InventoryEntry Main, Item item)
        {
            bool flag1 = item.Tags != null && item.Tags.Contains("Totem") && Main.Content.Tags != null && Main.Content.Tags.Contains("Totem");
            if (flag1)
            {
                return true;
            }
            string itemtext = item.DisplayName ?? "";
            string instancetext = Main.Content.DisplayName ?? "";
            bool flag2 = (itemtext.Contains("图腾") || itemtext.Contains("Totem")) && (instancetext.Contains("图腾") || instancetext.Contains("Totem"));
            if (flag2)
            {
                return true;
            }
            return false;
        }
    }
}
