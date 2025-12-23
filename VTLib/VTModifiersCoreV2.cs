using Duckov.UI;
using Duckov.Utilities;
using HarmonyLib;
using ItemStatsSystem;
using ItemStatsSystem.Stats;
using Newtonsoft.Json;
using SodaCraft.Localizations;
using UnityEngine;
using VTModifiers.ThirdParty;
using VTModifiers.VTLib.Items;
using Random = UnityEngine.Random;

// ReSharper disable All

namespace VTModifiers.VTLib;

public class VTModifiersCoreV2 {
    public static Dictionary<string, VtMKey> keys = new();

    public static void InitData() {
        InitVtmKeys();
        if (ModifierData.Count == 0) {
            LoadFromConfig();
        }
    }
    
    

    public static bool IsModifierFixed(VtModifierV2 modifier) {
        return modifier.forceFixed ? true : VTSettingManager.Setting.FixMode;
    }

    public const string ItemTagMask = "FaceMask";
    public const string ItemTagHeadset = "Headset";
    public const string ItemTagArmor = "Armor";
    public const string ItemTagHelmet = "Helmat";
    public const string ItemTagBackpack = "Backpack";
    public const string ItemTagGun = "Gun";
    public const string ItemTagMelee = "MeleeWeapon";

    public static readonly string VariableVtModifierHashCode = "VT_MODIFIER";
    public static readonly string VariableVtModifierSeedHashCode = "VT_MODIFIER_SEED";
    public static readonly string VariableVtModifierDisplayHashCodeOld = "Top1_词缀";
    public static readonly string VariableVtModifierDisplayHashCode = "VTModifiers_Top1_词缀";
    public static readonly string VariableVtAuthorDisplayHashCode = "VTModifiers_Top1_词缀作者";
    public static readonly string VariableVtLevelDisplayHashCode = "VTModifiers_Top1_词缀等级";
    public static Dictionary<string, VtModifierV2> ModifierData = new();
    public static Dictionary<string, string> AuthorData = new();

    public static void PatchItemDisplayInfo(Item item, VtModifierV2 modifier) {
        if (!DisplayConnector.Connected) return;
        DisplayConnector.PatchItem(item, modifier);
        DisplayConnector.TryRefresh(item);
    }
    public static void CalcItemModifiers(Item item) {
        string modifier = item.GetString(VariableVtModifierHashCode);
        if (modifier == null) return;
        if (ModifierData.TryGetValue(modifier, out VtModifierV2 vtModifier)) {
            bool flag = false;
            Random.State originalState = Random.state;
            if (!IsModifierFixed(vtModifier)) {
                int modifierSeed = item.GetInt(VariableVtModifierSeedHashCode, -1);
                if (modifierSeed == -1) {
                    modifierSeed = Random.Range(0, 1000000);
                    item.SetInt(VariableVtModifierSeedHashCode, modifierSeed);
                }
                Random.InitState(modifierSeed);
            }

            foreach (string vtmKey in vtModifier.data.Keys) {
                float value = vtModifier.data[vtmKey];
                if (TryPatchModifier(item, vtModifier, vtmKey, value)) {
                    flag = true;
                    // Log($"注入了Modifier:{item.DisplayName}_{vtm}");
                }
            }

            if (flag) {
                item.Modifiers.ReapplyModifiers();
            }
            Random.state = originalState;
            PatchItemDisplayInfo(item, vtModifier);
        }
        else {
            Log($"找不到modifier:{modifier}");
        }
    }
    public const int MAGIC_ORDER = -1145141919;
    public static List<VTModifierGroup> ModifierGroups = new ();

    public static void LoadFromConfig() {
        ModifierData.Clear();
        AuthorData.Clear();
        ModifierGroups.Clear();
        VTModifiersUI.modifiers.Clear();

        string directoryPath1 = ModBehaviour.Instance._modifiersDirectoryPersistant;
        string directoryPath2 = ModBehaviour.Instance._modifiersDirectoryCustom;
        List<string> jsonFiles = new();
        if (Directory.Exists(directoryPath1)) {
            jsonFiles.AddRange(Directory.GetFiles(directoryPath1, "*.json"));
        }
        if (Directory.Exists(directoryPath2)) {
            jsonFiles.AddRange(Directory.GetFiles(directoryPath2, "*.json"));
        }
        int loadedCount = 0;
        foreach (string path in jsonFiles) { 
            try {
                string jsonContent = File.ReadAllText(path);
                VTModifierGroup group = JsonConvert.DeserializeObject<VTModifierGroup>(jsonContent);
                ModifierGroups.Add(group);
                VT.Log($"加载来自{group.author}的{group.key}词缀组...");
                foreach (VtModifierV2 vtModifier in group.modifiers.Values) {
                    if (ModifierData.ContainsKey(vtModifier.key)) {
                        VT.Log($"词缀键重复:{vtModifier.key}");
                        continue;
                    }

                    string author = group.author;
                    if (vtModifier.author != null) {
                        author = vtModifier.author;
                    }
                    AuthorData[vtModifier.key] = author;
                    ModifierData[vtModifier.key] = vtModifier;
                    VTModifiersUI.modifiers.Add(vtModifier.key);
                    loadedCount++;
                }
            }
            catch (Exception ex) {
                VT.Log($"ModifierJSON解析错误， {path}: {ex.Message}");
            }
        }
        
    }
    static Dictionary<string, VtModifierV2> FilterModifierData(Item item) {
        Dictionary<string, VtModifierV2> filtered = new();
        foreach (VTModifierGroup group in ModifierGroups) {
            if (group.isCommunity && !VTSettingManager.Setting.EnableCommunityModifiers) continue;
            if (group.key == "vt_magic" && (
                !VTSettingManager.Setting.EnableArcaneModifiers || !MagicConnector.Connected
            )) continue;

            bool isCard = IsModifiersCard(item);
            foreach (VtModifierV2 vtm in group.modifiers.Values) {
                if (isCard) {
                    switch (item.TypeID) {
                        case ItemUtil.MC_CARD_v1:
                            if (vtm.quality == 1 || vtm.quality == 2) {
                                filtered.Add(vtm.key, vtm);
                            }
                            break;
                        case ItemUtil.MC_CARD_v2:
                            if (vtm.quality == 3 || vtm.quality == 4) {
                                filtered.Add(vtm.key, vtm);
                            }
                            break;
                        case ItemUtil.MC_CARD_v3:
                            if (vtm.quality == 5 || vtm.quality == 6) {
                                filtered.Add(vtm.key, vtm);
                            }
                            break;
                    }
                } else if (vtm.CanPatchTo(item)) {
                    filtered.Add(vtm.key, vtm);
                }
            }
        }
        return filtered;
    }
    
    static string? GetAModifierByWeight(Item item) {
        int totalWeight = 0;
        Dictionary<string, VtModifierV2> filteredModifierData = FilterModifierData(item);
        foreach (VtModifierV2 modifier in filteredModifierData.Values) {
            totalWeight += modifier.weight;
        }

        int num = UnityEngine.Random.Range(0, totalWeight);
        int num2 = 0;
        foreach (KeyValuePair<string, VtModifierV2> kvp in filteredModifierData) {
            if (num > num2 && num <= num2 + kvp.Value.weight) {
                return kvp.Key;
            }
            num2 += kvp.Value.weight;
        }
        return null;
    }
    
    public static bool IsPatchedItem(Item item) {
        return item.GetString(VariableVtModifierHashCode) != null;
    }

    public static bool IsModMD(ModifierDescription modifierDescription) {
        return modifierDescription.Order == MAGIC_ORDER;
    }
    
    public static bool ItemCanBePatched(Item item) {
        return item.Tags.Contains(ItemTagGun)
               || item.Tags.Contains(ItemTagMelee)
               || item.Tags.Contains(ItemTagHelmet)
               || item.Tags.Contains(ItemTagArmor)
               || item.Tags.Contains(ItemTagMask)
               || item.Tags.Contains(ItemTagHeadset)
               || item.Tags.Contains(ItemTagBackpack)
               || IsModifiersCard(item);
    }
    public static void TryUnpatchItem(Item item) {
        string modifier = item.GetString(VariableVtModifierHashCode);
        if (modifier == null) return;
        CustomDataCollection variables = Traverse.Create(item).Field("variables").GetValue<CustomDataCollection>();
        if (variables != null) {
            VT.RemoveItemVariable(variables, VariableVtModifierHashCode);
            VT.RemoveItemVariable(variables, VariableVtModifierSeedHashCode);
            VT.RemoveItemVariable(variables, VariableVtModifierDisplayHashCodeOld);
            VT.RemoveItemVariable(variables, VariableVtModifierDisplayHashCode);
            VT.RemoveItemVariable(variables, VariableVtAuthorDisplayHashCode);
            VT.RemoveItemVariable(variables, VariableVtLevelDisplayHashCode);
        }

        int removedModifiersCount = 0;
        List<ModifierDescription> toRemove = new();
        if (item.Modifiers != null) {
            foreach (ModifierDescription md in item.Modifiers) {
                if (IsModMD(md)) {
                    toRemove.Add(md);
                    removedModifiersCount++;
                }
            }
            foreach (ModifierDescription md in toRemove) {
                item.Modifiers.Remove(md);
            }
            item.Modifiers.ReapplyModifiers();
        }
        Log($"移除词缀:{item.DisplayName}成功，同时移除了{removedModifiersCount}个原生Modifier");
    }

    public static bool IsModifiersCard(Item item) {
        return ItemUtil.MC_CARD_IDS.Contains(item.TypeID);
    }
    public static string PatchItem(Item item, Sources source) {
        if (ItemCanBePatched(item)) {
            if (!IsModifiersCard(item)) {
                switch (source) {
                    case Sources.LootBox:
                        if (!VT.Probability(VTSettingManager.Setting.LootBoxPatchedPercentage)) return null;
                        break;
                    case Sources.Enemy:
                        if (!VT.Probability(VTSettingManager.Setting.EnemyPatchedPercentage)) return null;
                        break;
                    case Sources.Craft:
                        if (!VT.Probability(VTSettingManager.Setting.CraftPatchedPercentage)) return null;
                        break;
                    case Sources.SCAV:
                        if (!VT.Probability(VTSettingManager.Setting.SCAVPercentage)) return null;
                        break;
                    case Sources.Card:
                        break;
                }
            }
            string? modifier = GetAModifierByWeight(item);
            if (modifier != null) {
                return PatchItem(item, source, modifier);
            }
            // Log($"未找到Modifier 无法Patch");
        }
        return null;
    }
    public static string PatchItem(Item item, Sources source, string modifier) {
        item.SetString(VariableVtModifierHashCode, modifier, true);
        // Tag vtTag = new Tag()
        // item.Tags.Add("vttag");
        string modifierDisplayName = modifier.ToPlainText();
        if (VTSettingManager.Setting.Debug)
            Log($"注入:{item.DisplayName}为{modifierDisplayName}, source:{source}");
        CalcItemModifiers(item);
        return modifier;
    }
    public static string PatchItemDisplayName(Item item) {
        return PatchItemDisplayName(item, item.DisplayName);
    }

    public static string PatchItemDisplayName(Item item, string before) {
        if (item == null) return "";
        string modifier = item.GetString(VariableVtModifierHashCode);
        if (modifier != null) {
            string modifierDisplayText = modifier.ToPlainText();
            return modifierDisplayText + " " + before;
        }
        return before;
    }
    public enum Sources {
        LootBox, //物资
        Enemy, //敌人AI
        Debug, //测试用
        Craft, //制作的
        SCAV, //SCAV
        Card, //Card
        Reforge, //重铸的
    }

    static bool TryPatchModifier(Item item, VtModifierV2 vtModifier, string key, float value) {
        if (!keys.ContainsKey(key)) return false;
        VtMKey vtMKey = keys[key];
        if (vtMKey.noHash) return false;

        ModifierTarget mt = ModifierTarget.Character; //枪械是self
        if (vtMKey.forceTarget.HasValue) mt = vtMKey.forceTarget.Value;
        string hash = vtMKey.GetHashForItem(item);
        
        //特殊hash
        int polarity = 1;
        if (IsModifiersCard(item)) {
            mt = ModifierTarget.Self;
        }
        else if (item.Tags.Contains(ItemTagGun)) {
            if (!vtMKey.applyOnGuns) return false;
            //武器
            mt = ModifierTarget.Self;
        }
        else if (item.Tags.Contains(ItemTagMelee)) {
            if (!vtMKey.applyOnMelee) return false;
            //近战
            mt = ModifierTarget.Self;
        }
        else {
            if (!vtMKey.applyOnEquipments) return false;
            //元素类出现在护甲，正负逆转
            string[] elements = {
                VtmElementFire,
                VtmElementSpace,
                VtmElementPoison,
                VtmElementElectricity,
                VtmElementIce,
                VtmElementGhost,
                VtmPhysicFactor,
            };
            if (elements.Contains(key)) {
                polarity = -1;
            }
        }
        
        //特殊
        if (key == VtmDamageMultiplier) {
            if (value > 0) value *= VTSettingManager.Setting.DamageThreshold;
            else if (value < 0) value /= VTSettingManager.Setting.DamageThreshold;
        }
        if (key == VtmArmor) {
            if (value > 0) value *= VTSettingManager.Setting.ArmorThreshold;
            else if (value < 0) value /= VTSettingManager.Setting.ArmorThreshold;
        }
        value *= polarity;
        if (item.Modifiers == null) {
            item.CreateModifiersComponent();
            ModifierDescriptionCollection mdc = item.Modifiers;
            Traverse.Create(mdc).Field("list").SetValue(new List<ModifierDescription>());
        }
        if (item.Modifiers == null) return false;
        if (item.Modifiers.Find(tmd => (tmd.Key == hash && IsModMD(tmd))) != null) {
            //避免重复添加
            return false;
        }

        
        if (!IsModifierFixed(vtModifier) && !vtMKey.forceFixed) {
            value *= Random.Range(0.5f, 1f);
            if (vtMKey.roundToInt) {
                value = Mathf.Round(value);
            }
        }

        ModifierDescription md = new ModifierDescription(
            mt,
            hash,
            vtMKey.modifierType,
            value,
            false,
            MAGIC_ORDER
        );
        Traverse tmp = Traverse.Create(md);
        tmp.Field("display").SetValue(true);
        item.Modifiers.Add(md);
        return true;
    }

    public static float? GetItemVtmKey(Item item, string key) {
        string modifier = item.GetString(VariableVtModifierHashCode);
        if (modifier != null && ModifierData.TryGetValue(modifier, out var modifierStruct)) {
            return modifierStruct.data.GetValueOrDefault(key);
        }
        return null;
    }
    public static float Modify(Item item, string key, float original = 0f) {
        if (!item) return original;
        if (!keys.ContainsKey(key)) return original;
        VtMKey vtMKey = keys[key];
        string modifier = item.GetString(VariableVtModifierHashCode);
        
        if (modifier != null && ModifierData.TryGetValue(modifier, out var modifierStruct)) {
            if (vtMKey.forceFixed) {
                //直接获取，更快更稳
                if (modifierStruct.data.TryGetValue(key, out var val)) {
                    return vtMKey.PatchCustom(original, val);
                }
            }
            else {
                //从modifier获取
                ModifierDescriptionCollection mdc = item.Modifiers;
                if (mdc) {
                    string hash = vtMKey.hash;
                    ModifierDescription md = mdc.Find(md => (md.Key == hash));
                    if (md != null) {
                        return vtMKey.PatchCustom(original, md.Value);
                    }
                }
            }
        }
        return original;
    }
    public static int ReforgePrice(Item item) {
        bool isPatched = IsPatchedItem(item);
        float plus = isPatched
            ? VTSettingManager.Setting.ReforgePriceFactor
            : VTSettingManager.Setting.ForgePriceFactor; //词缀化需要5倍
        if (!isPatched && IsModifiersCard(item)) return 0;
        return Mathf.RoundToInt(Modify(item, VtmPriceMultiplier, item.Value) * plus);
    }
    
    public static void Log(string message, bool isError = false) {
        ModBehaviour.LogStatic(message, isError);
    }
    
    
    // public const string VtmDamage = "Damage"; //伤害修正
    public const string VtmDamageMultiplier = "DamageMultiplier"; //乘算的

    public const string VtmBulletSpeedMultiplier = "BulletSpeedMultiplier";
    public const string VtmShootDistanceMultiplier = "ShootDistanceMultiplier"; //射程
    public const string VtmShootSpeedMultiplier = "ShootSpeedMultiplier"; //射速/使用速度(近战)
    public const string VtmCritDamageMultiplier = "CritDamageMultiplier"; //爆伤因子,乘算
    public const string VtmArmorPiercing = "ArmorPiercing"; //穿甲等级 实际用整数
    public const string VtmPenetrate = "Penetrate"; //穿透 实际证书
    public const string VtmSoundRange = "SoundRange"; //声音，仅枪械
    public const string VtmReloadTimeMultiplier = "ReloadTimeMultiplier"; //换弹速度
    public const string VtmScatterMultiplier = "ScatterMultiplier";
    public const string VtmScatterADSAMultiplier = "ScatterADSMultiplier";
    public const string VtmRecoilVMultiplier = "RecoilVMultiplier";
    public const string VtmRecoilHMultiplier = "RecoilHMultiplier";
    public const string VtmTraceAbility = "TraceAbility"; //追踪
    public const string VtmBurstCount = "BurstCount"; //连发数
    public const string VtmShotCount = "ShotCount"; //多重射击
    public const string VtmShootSpeedGainEachShoot = "ShootSpeedGainEachShoot"; //射速叠加
    public const string VtmShootSpeedGainByShootMax = "ShootSpeedGainByShootMax"; //射速叠加上限


    //护甲
    public const string VtmArmor = "Armor";
    public const string VtmBodyArmor = "BodyArmor";
    public const string VtmHeadArmor = "HeadArmor";
    public const string VtmViewAngle = "ViewAngle";
    public const string VtmGasMask = "GasMask";
    public const string VtmInventoryCapacity = "InventoryCapacity";
    public const string VtmMaxWeight = "MaxWeight";
    public const string VtmMoveability = "Moveability";
    public const string VtmRunAcc = "RunAcc";
    public const string VtmSenseRange = "SenseRange";
    public const string VtmWalkSoundRange = "WalkSoundRange"; 
    public const string VtmRunSoundRange = "RunSoundRange"; 
    public const string VtmColdProtection = "ColdProtection"; 
    public const string VtmStormProtection = "StormProtection"; 
    
    //特殊
    public const string VtmBleedChance = "BleedChance"; //流血几率
    public const string VtmWeight = "Weight";
    public const string VtmAmmoSave = "AmmoSave";
    public const string VtmPriceMultiplier = "PriceMultiplier";
    public const string VtmLifeSteal = "LifeSteal";  
    public const string VtmDodgeRate = "DodgeRate"; //闪避概率(护甲)
    public const string VtmDeathRate = "DeathRate"; //即死(99999)
    public const string VtmEndurance = "Endurance"; //概率不消耗耐久

    //元素
    public const string VtmElementFire = "ElementFire";
    public const string VtmElementSpace = "ElementSpace";
    public const string VtmElementPoison = "ElementPoison";
    public const string VtmElementElectricity = "ElementElectricity";
    public const string VtmElementGhost = "ElementGhost";
    public const string VtmElementIce = "ElementIce";
    public const string VtmPhysicFactor = "ElementPhysic"; //物理承伤倍率
    
    //秘法
    public const string VtmMagicPower = "MagicPower";
    public const string VtmMaxMana = "MaxMana";
    public const string VtmManaCost = "ManaCost";
    public const string VtmCastTime = "CastTime";
    public const string VtmMagicCritRate = "MagicCritRate";

    //近战
    public const string VtmStaminaCost = "StaminaCost";
    public const string VtmMaxStamina = "MaxStamina";
    public const string VtmCritRate = "CritRate"; //暴击率,枪械暂未使用，因为官方写死了爆头才算暴击

    
    
    public static void InitVtmKeys() {
        keys.Clear();
        AddKey(new VtMKey(VtmDamageMultiplier, nameof(ItemAgent_Gun.Damage)) {
            applyOnGuns = true,
            applyOnMelee = true,
        });
        AddKey(new VtMKey(VtmBulletSpeedMultiplier, nameof(ItemAgent_Gun.BulletSpeed)) {
            applyOnGuns = true,
            applyOnEquipments = true,
            hashForEquipments = "BulletSpeedMultiplier",
        });
        AddKey(new VtMKey(VtmCritDamageMultiplier, nameof(ItemAgent_Gun.CritDamageFactor)) {
            applyOnGuns = true,
            applyOnEquipments = true,
            applyOnMelee = true,
            hashForEquipments = nameof(CharacterMainControl.GunCritDamageGain),
            modifierType = ModifierType.Add,
        });
        
        AddKey(new VtMKey(VtmShootDistanceMultiplier, nameof(ItemAgent_Gun.BulletDistance)) {
            applyOnGuns = true,
            applyOnMelee = true,
            hashForMelee = nameof(ItemAgent_MeleeWeapon.AttackRange),
        });
        AddKey(new VtMKey(VtmShootSpeedMultiplier, nameof(ItemAgent_Gun.ShootSpeed)) {
            applyOnGuns = true,
            applyOnMelee = true,
            hashForMelee = nameof(ItemAgent_MeleeWeapon.AttackSpeed),
        });

        AddKey(new VtMKey(VtmReloadTimeMultiplier, nameof(ItemAgent_Gun.ReloadTime)){applyOnGuns = true});
        AddKey(new VtMKey(VtmAmmoSave, "VTMC_" + VtmAmmoSave) {
            applyOnGuns = true, 
            modifierType = ModifierType.Add,
            isCustom = true,
        });
        AddKey(new VtMKey(VtmLifeSteal, "VTMC_" + VtmLifeSteal) {
            applyOnGuns = true,
            applyOnMelee = true,
            modifierType = ModifierType.Add,
            modifierTypeCustom = ModifierType.Add,
            isCustom = true,
        });
        AddKey(new VtMKey(VtmEndurance, "VTMC_" + VtmEndurance) {
            applyOnEquipments = true,
            modifierType = ModifierType.Add,
            modifierTypeCustom = ModifierType.Add,
            isCustom = true,
        });
        AddKey(new VtMKey(VtmDeathRate, "VTMC_" + VtmDeathRate) {
            applyOnGuns = true,
            applyOnMelee = true,
            modifierType = ModifierType.Add,
            modifierTypeCustom = ModifierType.Add,
            isCustom = true,
        });
        AddKey(new VtMKey(VtmDodgeRate, "VTMC_" + VtmDodgeRate) {
            applyOnEquipments = true,
            modifierType = ModifierType.Add,
            modifierTypeCustom = ModifierType.Add,
            isCustom = true,
        });
        AddKey(new VtMKey(VtmArmorPiercing, nameof(ItemAgent_Gun.ArmorPiercing)) {
            applyOnGuns = true, 
            applyOnMelee = true,
            modifierType = ModifierType.Add,
            roundToInt = true,
        });
        AddKey(new VtMKey(VtmPenetrate, nameof(ItemAgent_Gun.Penetrate)) {
            applyOnGuns = true, 
            modifierType = ModifierType.Add,
            roundToInt = true,
        });
        AddKey(new VtMKey(VtmSoundRange, nameof(ItemAgent_Gun.SoundRange)){applyOnGuns = true});
        AddKey(new VtMKey(VtmRecoilHMultiplier, nameof(ItemAgent_Gun.RecoilScaleH)){applyOnGuns = true});
        AddKey(new VtMKey(VtmRecoilVMultiplier, nameof(ItemAgent_Gun.RecoilScaleV)){applyOnGuns = true});
        AddKey(new VtMKey(VtmScatterMultiplier, "ScatterFactor"){applyOnGuns = true});
        AddKey(new VtMKey(VtmScatterADSAMultiplier, "ScatterFactorADS"){applyOnGuns = true});
        AddKey(new VtMKey(VtmTraceAbility, nameof(ItemAgent_Gun.TraceAbility)) {
            applyOnGuns = true,
            modifierType = ModifierType.Add,
        });
        AddKey(new VtMKey(VtmBurstCount, nameof(ItemAgent_Gun.BurstCount)) {
            applyOnGuns = true,
            modifierType = ModifierType.Add,
            forceFixed = true,
            roundToInt = true,
        });
        AddKey(new VtMKey(VtmShotCount, nameof(ItemAgent_Gun.ShotCount)) {
            applyOnGuns = true,
            modifierType = ModifierType.Add,
            forceFixed = true,
            roundToInt = true,
        });
        AddKey(new VtMKey(VtmShootSpeedGainEachShoot, nameof(ItemAgent_Gun.ShootSpeedGainEachShoot)) {
            applyOnGuns = true,
            modifierType = ModifierType.Add,
        });
        AddKey(new VtMKey(VtmShootSpeedGainByShootMax, nameof(ItemAgent_Gun.ShootSpeedGainByShootMax)) {
            applyOnGuns = true,
            modifierType = ModifierType.Add,
        });
        
        

        AddKey(new VtMKey(VtmBleedChance, "VTMC_" + VtmBleedChance) {
            applyOnGuns = true,
            applyOnMelee = true,
            hashForMelee = nameof(ItemAgent_MeleeWeapon.BleedChance),
            modifierTypeCustom = ModifierType.Add,
            isCustom = true,
        });
        AddKey(new VtMKey(VtmElementElectricity, "VTMC_" + VtmElementElectricity) {
            applyOnGuns = true, 
            applyOnEquipments = true,
            modifierType = ModifierType.Add,
            hashForEquipments = "ElementFactor_Electricity",
            modifierTypeCustom = ModifierType.Add,
            isCustom = true,
        });
        AddKey(new VtMKey(VtmElementIce, "VTMC_" + VtmElementIce) {
            applyOnGuns = true, 
            applyOnEquipments = true,
            modifierType = ModifierType.Add,
            hashForEquipments = "ElementFactor_Ice",
            modifierTypeCustom = ModifierType.Add,
            isCustom = true,
        });
        AddKey(new VtMKey(VtmElementGhost, "VTMC_" + VtmElementGhost) {
            applyOnGuns = true, 
            applyOnEquipments = true,
            modifierType = ModifierType.Add,
            hashForEquipments = "ElementFactor_Ghost",
            modifierTypeCustom = ModifierType.Add,
            isCustom = true,
        });
        AddKey(new VtMKey(VtmElementFire, "VTMC_" + VtmElementFire) {
            applyOnGuns = true, 
            applyOnEquipments = true,
            modifierType = ModifierType.Add,
            hashForEquipments = "ElementFactor_Fire",
            modifierTypeCustom = ModifierType.Add,
            isCustom = true,
        });
        AddKey(new VtMKey(VtmElementSpace, "VTMC_" + VtmElementSpace) {
            applyOnGuns = true, 
            applyOnEquipments = true,
            modifierType = ModifierType.Add,
            hashForEquipments = "ElementFactor_Space",
            modifierTypeCustom = ModifierType.Add,
            isCustom = true,
        });
        AddKey(new VtMKey(VtmElementPoison, "VTMC_" + VtmElementPoison) {
            applyOnGuns = true, 
            applyOnEquipments = true,
            modifierType = ModifierType.Add,
            hashForEquipments = "ElementFactor_Poison",
            modifierTypeCustom = ModifierType.Add,
            isCustom = true,
        });
        AddKey(new VtMKey(VtmPhysicFactor, "ElementFactor_Physics") {
            applyOnEquipments = true, 
            modifierType = ModifierType.Add,
        });
        
        AddKey(new VtMKey(VtmArmor, "Armor"){applyOnEquipments = true, modifierType = ModifierType.Add});
        AddKey(new VtMKey(VtmInventoryCapacity, "InventoryCapacity") {
            applyOnEquipments = true, 
            modifierType = ModifierType.Add,
            roundToInt = true,
        });
        AddKey(new VtMKey(VtmGasMask, "GasMask") {
            applyOnEquipments = true, 
            modifierType = ModifierType.Add,
        });
        AddKey(new VtMKey(VtmMaxWeight, "MaxWeight"){applyOnEquipments = true, modifierType = ModifierType.Add});
        AddKey(new VtMKey(VtmMoveability, nameof (ItemAgent_Gun.MoveSpeedMultiplier)) {
            applyOnGuns = true,
            applyOnEquipments = true, 
            applyOnMelee = true,
            hashForEquipments = "Moveability",
            modifierType = ModifierType.Add
        });
        AddKey(new VtMKey(VtmRunAcc, "RunAcc") {
            applyOnGuns = true,
            applyOnEquipments = true, 
            applyOnMelee = true,
            modifierType = ModifierType.PercentageAdd,
            forceTarget = ModifierTarget.Character,
        });
        
        AddKey(new VtMKey(VtmViewAngle, "ViewAngle") {
            applyOnEquipments = true
        });
        AddKey(new VtMKey(VtmWalkSoundRange, nameof (CharacterMainControl.WalkSoundRange)){applyOnEquipments = true});
        AddKey(new VtMKey(VtmRunSoundRange, nameof (CharacterMainControl.RunSoundRange)){applyOnEquipments = true});
        AddKey(new VtMKey(VtmColdProtection, nameof (CharacterMainControl.ColdProtection)) {
            applyOnEquipments = true,
            modifierType = ModifierType.Add
        });
        AddKey(new VtMKey(VtmStormProtection, nameof (CharacterMainControl.StormProtection)) {
            applyOnEquipments = true,
            modifierType = ModifierType.Add
        });
        AddKey(new VtMKey(VtmMaxStamina, "Stamina"){applyOnEquipments = true, modifierType = ModifierType.Add});
        AddKey(new VtMKey(VtmSenseRange, nameof(CharacterMainControl.SenseRange)) {
            applyOnGuns = true,
            applyOnEquipments = true,
            forceTarget = ModifierTarget.Character,
            modifierType = ModifierType.Add,
        });
        
        AddKey(new VtMKey(VtmCritRate, nameof(ItemAgent_MeleeWeapon.CritRate)){applyOnMelee = true, modifierType = ModifierType.Add});
        AddKey(new VtMKey(VtmStaminaCost, nameof(ItemAgent_MeleeWeapon.StaminaCost)){applyOnMelee = true});

        
        AddKey(new VtMKey(VtmPriceMultiplier, "") {
            applyOnGuns = true,
            applyOnEquipments = true,
            applyOnMelee = true,
            modifierType = ModifierType.Add,
            isCustom = true,
            noHash = true,
            forceFixed = true,
        });
        AddKey(new VtMKey(VtmWeight, "VTMC_" + VtmWeight) {
            applyOnGuns = true,
            applyOnEquipments = true,
            applyOnMelee = true,
            modifierType = ModifierType.Add,
            isCustom = true,
        });
        
        //秘法
        AddKey(new VtMKey(VtmMagicPower, CharacterMagicStats.MagicPower) {
            applyOnEquipments = true,
            modifierType = ModifierType.Add,
        });
        AddKey(new VtMKey(VtmMagicCritRate, CharacterMagicStats.MagicCritRate) {
            applyOnEquipments = true,
            modifierType = ModifierType.Add,
        });
        AddKey(new VtMKey(VtmMaxMana, CharacterMagicStats.MaxMana) {
            applyOnEquipments = true,
            roundToInt = true,
            modifierType = ModifierType.Add,
        });
        AddKey(new VtMKey(VtmCastTime, CharacterMagicStats.CastTime) {
            applyOnEquipments = true,
            modifierType = ModifierType.Add,
        });
        AddKey(new VtMKey(VtmManaCost, CharacterMagicStats.ManaCost) {
            applyOnEquipments = true,
            modifierType = ModifierType.Add,
        });
        
        void AddKey(VtMKey key) {
            keys[key.key] = key;
        }
    }
    
    public struct VtMKey {
        public string key; //唯一键
        public string hash; //哈希

        public string GetHashForItem(Item item) {
            if (IsModifiersCard(item)) return hash;
            if (item.Tags.Contains(ItemTagMelee)) {
                if (hashForMelee != null) {
                    return hashForMelee;
                }
            }
            else if (item.Tags.Contains(ItemTagGun)) {
            }
            else {
                if (hashForEquipments != null) {
                    return hashForEquipments;
                }
                if (hash == "Armor") {
                    if (item.Tags.Contains(ItemTagHelmet) || item.Tags.Contains(ItemTagMask) || item.Tags.Contains(ItemTagHeadset)) {
                        return nameof(Health.HeadArmor);
                    }
                    if (item.Tags.Contains(ItemTagArmor) || item.Tags.Contains(ItemTagBackpack)) {
                        return nameof(Health.BodyArmor);
                    }
                }
            }
            return hash;
        }

        public bool noHash = false; //特殊键，不需要hash(如价格，重量等）
        public ModifierType modifierType = ModifierType.PercentageAdd;
        //自定义的参数的效果
        public bool isCustom = false; //自定义Stat，不来自原生的Stat
        public bool forceFixed = false; //是否强制最大值
        public ModifierType modifierTypeCustom = ModifierType.PercentageAdd;
        public ModifierTarget? forceTarget = null;
        public bool roundToInt = false; //是否整数

        public bool applyOnGuns = false;
        public bool applyOnMelee = false;
        public string? hashForMelee = null;
        public bool applyOnEquipments = false;
        public string? hashForEquipments = null;

        public float PatchCustom(float original, float value) {
            switch (modifierTypeCustom) {
                case ModifierType.Add:
                    return original + value;
                case ModifierType.PercentageAdd:
                    return original * (1f + value);
            }
            return original;
        }
        public VtMKey(string key, string hash) {
            this.key = key;
            this.hash = hash;
        }
    }

    public static void PatchByCard(Item card, Item item, ItemDisplay itemDisplay) {
        // VT.Log("尝试打词缀卡");
        if (IsModifiersCard(item)) {
            //不能给卡上卡
            VT.BubbleUserDebug("Bubble_cannot_patch_to_card".ToPlainText());
            VT.PostCustomSFX("Terraria_no.wav");
            return;
        }
        string cardModifier = card.GetString(VariableVtModifierHashCode);
        if (cardModifier == null) {
            VT.BubbleUserDebug("Bubble_cannot_patch_empty_card".ToPlainText());
            VT.PostCustomSFX("Terraria_no.wav");
            return;
        }
        if (!ModifierData.TryGetValue(cardModifier, out VtModifierV2 vtModifierV2)) {
            VT.PostCustomSFX("Terraria_no.wav");
            return;
        }

        if (!vtModifierV2.CanPatchTo(item)) {
            VT.BubbleUserDebug("Bubble_cannot_patch_to_item".ToPlainText());
            VT.PostCustomSFX("Terraria_no.wav");
            return;
        }
        int cardModifierSeed = card.GetInt(VariableVtModifierSeedHashCode, -1);
        if (cardModifierSeed != -1) {
            item.SetInt(VariableVtModifierSeedHashCode, cardModifierSeed);
        }
        TryUnpatchItem(item);
        PatchItem(item, Sources.Card, cardModifier);
        card.Detach();
        VT.PostCustomSFX("Terraria_card_patch.wav");
        VT.ForceUpdateItemDisplayName(itemDisplay);
    }
    
    public struct VTModifierGroup {
        public string key;
        public string author = "Official";
        public string version = "0.0.1"; 
        public bool isCommunity = false; 
        public Dictionary<string, VtModifierV2> modifiers = new();

        public VTModifierGroup(string key) {
            this.key = key;
        }
    }
    public struct VtModifierV2 {
        public string key;
        public string? author = null; //作者名
        public int weight = 0; //权重
        public int quality = 1; //等级从-6到10 0代表不强不弱 负的代表负面的
        public bool forceFixed = false;

        public bool applyOnGuns = false; 
        public bool applyOnMelee = false; 
        public bool applyOnEquipment = false; 
        public bool applyOnHelmet = false;
        public bool applyOnArmor = false;
        public bool applyOnFaceMask = false;
        public bool applyOnHeadset = false;
        public bool applyOnBackpack = false;

        public bool CanPatchTo(Item item) {
            return (item.Tags.Contains(ItemTagGun) && applyOnGuns)
                   || (item.Tags.Contains(ItemTagMelee) && applyOnMelee)
                   || (item.Tags.Contains(ItemTagArmor) && (applyOnArmor || applyOnEquipment))
                   || (item.Tags.Contains(ItemTagHelmet) && (applyOnHelmet || applyOnEquipment))
                   || (item.Tags.Contains(ItemTagHeadset) && (applyOnHeadset || applyOnEquipment))
                   || (item.Tags.Contains(ItemTagMask) && (applyOnFaceMask || applyOnEquipment))
                   || (item.Tags.Contains(ItemTagBackpack) && (applyOnBackpack || applyOnEquipment));
        }
        //核心数据，需要注意，这里面的键不能手动输入，而是一个固定的select
        public Dictionary<string, float> data = new();

        public VtModifierV2(string key) {
            this.key = key;
        }
    }
    
}