using System.Reflection;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ItemStatsSystem;
using VTModifiers.VTLib;

namespace VTModifiers.ThirdParty {
    public class LootBoxEventListener : MonoBehaviour {
        private static LootBoxEventListener _instance;
        private static EventInfo? _lootBoxEvent;
        private static MethodInfo? _eventSubscribeMethod;
        private static MethodInfo? _eventUnsubscribeMethod;

        public static LootBoxEventListener Instance {
            get {
                if (_instance == null) {
                    GameObject go = new GameObject("LootBoxEventListener");
                    _instance = go.AddComponent<LootBoxEventListener>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        void Start() {
            Invoke(nameof(InitializeListener), 1.0f);
        }

        private void InitializeListener() {
            try {
                Assembly randomNpcAssembly = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    if (assembly.GetName().Name == "RandomNpc") {
                        randomNpcAssembly = assembly;
                        break;
                    }
                }

                if (randomNpcAssembly == null) {
                    // ModBehaviour.LogStatic("用户未安装SCAV mod");
                    VTSettingManager._scavLoaded = false;
                    return;
                }

                Type broadcasterType = randomNpcAssembly.GetType("RandomNpc.LootBoxEventBroadcaster");
                if (broadcasterType == null) {
                    ModBehaviour.LogStatic("未找到LootBoxEventBroadcaster类型");
                    return;
                }

                _lootBoxEvent = broadcasterType.GetEvent("OnLootBoxModified");
                if (_lootBoxEvent == null) {
                    ModBehaviour.LogStatic("未找到OnLootBoxModified事件");
                    return;
                }

                MethodInfo handlerMethod = typeof(LootBoxEventListener).GetMethod("OnLootBoxModified",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                // 创建委托
                Delegate handler = Delegate.CreateDelegate(_lootBoxEvent.EventHandlerType, this, handlerMethod);

                _eventSubscribeMethod = broadcasterType.GetMethod("add_OnLootBoxModified");
                _eventSubscribeMethod.Invoke(null, new object[] { handler });

                ModBehaviour.LogStatic("检测到用户安装SCAV mod, 联动订阅");
                VTSettingManager._scavLoaded = true;
                ModSettingConnector.InitSCAV();
            }
            catch (Exception ex) {
                ModBehaviour.LogStatic($"订阅战利品箱修改事件失败: {ex.Message}");
                // 如果失败，尝试重新初始化
                Invoke(nameof(InitializeListener), 2.0f);
            }
        }

        void OnDestroy() {
            try {
                if (_eventSubscribeMethod != null && _lootBoxEvent != null) {
                    // 获取当前类的方法信息
                    MethodInfo handlerMethod = typeof(LootBoxEventListener).GetMethod("OnLootBoxModified",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    // 创建委托
                    Delegate handler = Delegate.CreateDelegate(_lootBoxEvent.EventHandlerType, this, handlerMethod);

                    // 获取取消订阅方法
                    _eventUnsubscribeMethod = _eventSubscribeMethod.DeclaringType.GetMethod("remove_OnLootBoxModified");

                    // 取消订阅事件
                    _eventUnsubscribeMethod?.Invoke(null, new object[] { handler });

                    ModBehaviour.LogStatic("已通过反射取消订阅战利品箱修改事件");
                }
            }
            catch (Exception ex) {
                ModBehaviour.LogStatic($"取消订阅战利品箱修改事件失败: {ex.Message}");
            }
        }

        private void OnLootBoxModified(InteractableLootbox lootBox) {
            if (lootBox == null) {
                ModBehaviour.LogStatic("[LootBoxEventListener] 接收到空的战利品箱对象");
                return;
            }

            try {
                if (lootBox.Inventory) {
                    foreach (Item item in lootBox.Inventory) {
                        VTModifiersCoreV2.PatchItem(item, VTModifiersCoreV2.Sources.SCAV);
                    }
                }
                // ModBehaviour.LogStatic($"[LootBoxEventListener] 已获取lootbox");
            }
            catch (Exception ex) {
                ModBehaviour.LogStatic($"[LootBoxEventListener] 处理战利品箱修改事件时出错: {ex.Message}");
            }
        }
        //
        // private async UniTask AddItemToLootBox(InteractableLootbox lootBox, int itemID) {
        //     try {
        //         Item newItem = await ItemAssetsCollection.InstantiateAsync(itemID);
        //         if (newItem == null) {
        //             ModBehaviour.LogStatic($"[LootBoxEventListener] 无法创建ID为{itemID}的物品");
        //             return;
        //         }
        //
        //         var inventory = lootBox.Inventory;
        //         if (inventory == null) {
        //             ModBehaviour.LogStatic("[LootBoxEventListener] 战利品箱库存为空");
        //             return;
        //         }
        //
        //         MethodInfo addMethod = inventory.GetType().GetMethod("AddItem");
        //         if (addMethod != null) {
        //             addMethod.Invoke(inventory, new object[] { newItem });
        //             ModBehaviour.LogStatic($"[LootBoxEventListener] 已向战利品箱添加ID为{itemID}的物品: {newItem.DisplayName}");
        //         }
        //         else {
        //             ModBehaviour.LogStatic("[LootBoxEventListener] 无法找到AddItem方法");
        //         }
        //     }
        //     catch (Exception ex) {
        //         ModBehaviour.LogStatic($"[LootBoxEventListener] 向战利品箱添加物品时出错: {ex.Message}");
        //     }
        // }
    }
}