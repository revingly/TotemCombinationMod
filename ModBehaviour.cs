using HarmonyLib;
using UnityEngine;

namespace TotemCombination
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private Harmony? harmony;
        private const string id = "REV.TotemCombination";
        public static string modName = "TotemCombination";

        private void Awake() => Debug.Log((object)$"[{modName}] Mod Awake");

        private void OnEnable()
        {
            Debug.Log((object)$"[{modName}] OnEnable");
            this.harmony = new Harmony(id);
            this.harmony.PatchAll();
        }

        private void OnDisable()
        {
            Debug.Log((object)$"[{modName}] OnDisable");
            this.harmony?.UnpatchAll(id);
        }

        private void OnDestroy() => Debug.Log((object)$"[{modName}] OnDestroy");
    }
}
