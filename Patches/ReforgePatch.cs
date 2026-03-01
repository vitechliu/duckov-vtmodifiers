using Duckov.Economy;
using Duckov.UI;
using HarmonyLib;
using ItemStatsSystem;
using SodaCraft.Localizations;
using UnityEngine;
using UnityEngine.UI;
using VTLib;
using VTModifiers.VTLib;
using Object = UnityEngine.Object;

namespace VTModifiers.Patches; 

[HarmonyPatch]
public static class ReforgePatch {
    //重铸
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemOperationMenu), "Initialize")]
    public static void ItemOperationMenu_Initialize_PostFix(ItemOperationMenu __instance) {
        if (VTMO.btn_Reforge == null) {
            Button btnSample = __instance.btn_Equip;
            if (btnSample == null) return;
            GameObject newBtn = Object.Instantiate(btnSample.gameObject, btnSample.transform.parent);
            VTMO.btn_Reforge = newBtn.GetComponent<Button>();
            VTMO.btn_Reforge.name = "Btn_Reforge";

            VTMO.btn_Reforge.onClick.RemoveAllListeners();
            VTMO.btn_Reforge.onClick.AddListener(OnReforge);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemOperationMenu), "Setup")]
    public static void ItemOperationMenu_Setup_PostFix(ItemOperationMenu __instance) {
        if (VTMO.btn_Reforge) {
            if (LevelManager.Instance.IsBaseLevel) {
                Item targetItem = __instance.TargetItem;
                if (targetItem && VTModifiersCoreV2.ItemCanBePatched(targetItem)) {
                    bool patched = VTModifiersCoreV2.IsPatchedItem(targetItem);
                    if ((patched && VTSettingManager.Setting.AllowReforge)
                        || (!patched && VTSettingManager.Setting.AllowForge)) {
                        VTMO.btn_Reforge.gameObject.SetActive(true);
                        EnsureButtonStyle(targetItem);
                        return;
                    }
                }
            }

            VTMO.btn_Reforge.gameObject.SetActive(false);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemOperationMenu), "OnOpen")]
    public static void ItemOperationMenu_OnOpen_PostFix(ItemOperationMenu __instance) {
        if (VTMO.btn_Reforge) {
            if (LevelManager.Instance.IsBaseLevel) {
                Item targetItem = __instance.TargetItem;
                if (targetItem && VTModifiersCoreV2.ItemCanBePatched(targetItem)) {
                    bool patched = VTModifiersCoreV2.IsPatchedItem(targetItem);
                    if ((patched && VTSettingManager.Setting.AllowReforge)
                        || (!patched && VTSettingManager.Setting.AllowForge)) {
                        EnsureButtonStyle(targetItem);
                    }
                }
            }
        }
    }

    static void EnsureButtonStyle(Item targetItem) {
        bool patched = VTModifiersCoreV2.IsPatchedItem(targetItem);
        int price = VTModifiersCoreV2.ReforgePrice(targetItem);
        long userMoney = EconomyManager.Money;
        string buttonText = patched ? "Btn_reforge".ToPlainText() : "Btn_forge".ToPlainText();
        VT.SetButtonText(VTMO.btn_Reforge, buttonText + $"(${price})");

        if (userMoney >= price) {
            VTMO.btn_Reforge.interactable = true;
            VT.SetButtonColor(VTMO.btn_Reforge, new Color(0.6f, 0f, 0.7f));
        }
        else {
            VTMO.btn_Reforge.interactable = false;
            VT.SetButtonColor(VTMO.btn_Reforge, new Color(0.8f, 0.4f, 0.9f));
        }
    }

    public static void KeyReforge() {
        Item targetItem = ItemUIUtilities.SelectedItem;
        ItemDisplay display = ItemUIUtilities.SelectedItemDisplay;

        if (!targetItem || !display) {
            VT.BubbleUserDebug("Bubble_no_item_select".ToPlainText());
            return;
        }

        if (!VTModifiersCoreV2.ItemCanBePatched(targetItem)) return;
        int price = VTModifiersCoreV2.ReforgePrice(targetItem);
        if (!EconomyManager.Pay(new Cost(price))) {
            VT.BubbleUserDebug("Bubble_lack_of_coin".ToPlainText());
            return;
        }

        VTModifiersCoreV2.TryUnpatchItem(targetItem);
        VTModifiersCoreV2.PatchItem(targetItem, VTModifiersCoreV2.Sources.Reforge);
        
        if (VTSettingManager.Setting.ReforgeSound)
            VTMO.PostCustomSFX("Terraria_reforging.wav");
        VT.BubbleUserDebug("Bubble_reforge_success".ToPlainText());

        //更新仓库里面的名称
        display.nameText.text = display.Target.DisplayName;
    }

    public static void OnReforge() {
        ItemOperationMenu __instance = ItemOperationMenu.Instance;
        if (!__instance) return;
        Item targetItem = __instance.TargetItem;
        if (!targetItem) return;
        if (!VTModifiersCoreV2.ItemCanBePatched(targetItem)) return;

        int price = VTModifiersCoreV2.ReforgePrice(targetItem);
        if (!EconomyManager.Pay(new Cost(price))) {
            VT.BubbleUserDebug("Bubble_lack_of_coin".ToPlainText());
            __instance.Close();
            return;
        }


        VTModifiersCoreV2.TryUnpatchItem(targetItem);
        VTModifiersCoreV2.PatchItem(targetItem, VTModifiersCoreV2.Sources.Reforge);
        if (VTSettingManager.Setting.ReforgeSound)
            VTMO.PostCustomSFX("Terraria_reforging.wav");
        VT.BubbleUserDebug("Bubble_reforge_success".ToPlainText());
        __instance.Close();

        //更新仓库里面的名称
        ItemOperationMenu iom = ItemOperationMenu.Instance;
        if (iom) {
            ItemDisplay itemDisplay = iom.TargetDisplay;
            if (itemDisplay) itemDisplay.nameText.text = itemDisplay.Target.DisplayName;
        }
    }

}