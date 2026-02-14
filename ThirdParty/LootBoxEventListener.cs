using System.Reflection;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ItemStatsSystem;
using VTModifiers.VTLib;
using VTLib;

namespace VTModifiers.ThirdParty {
    public class LootBoxEventListener : MonoBehaviour {
        private static LootBoxEventListener _instance;
        private static readonly object _lock = new object();
        private static EventInfo? _lootBoxEvent;
        private static MethodInfo? _eventSubscribeMethod;
        private static MethodInfo? _eventUnsubscribeMethod;
        private static bool _isSubscribed = false;
        private static bool _initializationAttempted = false;

        public static LootBoxEventListener Instance {
            get {
                lock (_lock) {
                    if (_instance == null) {
                        GameObject go = new GameObject("LootBoxEventListener");
                        _instance = go.AddComponent<LootBoxEventListener>();
                        DontDestroyOnLoad(go);
                    }
                    return _instance;
                }
            }
        }

        void Start() {
            InitializeListener();
        }

        private void InitializeListener() {
            if (_isSubscribed) {
                return; // 防止重复订阅
            }
            try {
                Assembly randomNpcAssembly = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    if (assembly.GetName().Name == "RandomNpc") {
                        randomNpcAssembly = assembly;
                        break;
                    }
                }

                if (randomNpcAssembly == null) {
                    VTMO.Log("未找到RandomNpc程序集");
                    return;
                }

                Type broadcasterType = randomNpcAssembly.GetType("RandomNpc.LootBoxEventBroadcaster");
                if (broadcasterType == null) {
                    VTMO.Log("未找到LootBoxEventBroadcaster类型");
                    return;
                }

                _lootBoxEvent = broadcasterType.GetEvent("OnLootBoxModified");
                if (_lootBoxEvent == null) {
                    VTMO.Log("未找到OnLootBoxModified事件");
                    return;
                }

                _eventSubscribeMethod = broadcasterType.GetMethod("add_OnLootBoxModified");
                if (_eventSubscribeMethod == null) {
                    VTMO.Log("未找到add_OnLootBoxModified方法");
                    return;
                }

                _eventUnsubscribeMethod = broadcasterType.GetMethod("remove_OnLootBoxModified");
                if (_eventUnsubscribeMethod == null) {
                    VTMO.Log("未找到remove_OnLootBoxModified方法");
                    return;
                }

                MethodInfo handlerMethod = typeof(LootBoxEventListener).GetMethod("OnLootBoxModified",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (handlerMethod == null) {
                    VTMO.Log("未找到OnLootBoxModified处理方法");
                    return;
                }

                // 创建委托
                Delegate handler = Delegate.CreateDelegate(_lootBoxEvent.EventHandlerType, this, handlerMethod);

                // 订阅事件
                _eventSubscribeMethod.Invoke(null, new object[] { handler });
                _isSubscribed = true;

                VTMO.Log("SCAV事件已订阅");
                ModSettingConnector.TryInitSCAV();
            }
            catch (System.Reflection.ReflectionTypeLoadException ex) {
                VTMO.Log($"订阅战利品箱修改事件失败: {ex.Message}");
                foreach (var loaderEx in ex.LoaderExceptions) {
                    VTMO.Log($"加载异常: {loaderEx.Message}");
                }
            }
            catch (Exception ex) {
                VTMO.Log($"订阅战利品箱修改事件失败: {ex.Message}");
                // 如果失败，尝试重新初始化（限制重试次数）
                if (!_initializationAttempted) {
                    _initializationAttempted = true;
                    Invoke(nameof(InitializeListener), 2.0f);
                }
            }
        }

        void OnDestroy() {
            try {
                if (_isSubscribed && _eventSubscribeMethod != null && _lootBoxEvent != null && _eventUnsubscribeMethod != null) {
                    // 获取当前类的方法信息
                    MethodInfo handlerMethod = typeof(LootBoxEventListener).GetMethod("OnLootBoxModified",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (handlerMethod != null) {
                        // 创建委托
                        Delegate handler = Delegate.CreateDelegate(_lootBoxEvent.EventHandlerType, this, handlerMethod);

                        // 取消订阅事件
                        _eventUnsubscribeMethod.Invoke(null, new object[] { handler });
                        _isSubscribed = false;

                        VTMO.Log("已通过反射取消订阅战利品箱修改事件");
                    }
                }
            }
            catch (Exception ex) {
                VTMO.Log($"取消订阅战利品箱修改事件失败: {ex.Message}");
            }
        }

        private void OnLootBoxModified(InteractableLootbox lootBox) {
            if (lootBox == null) {
                VTMO.Log("[LootBoxEventListener] 接收到空的战利品箱对象");
                return;
            }
            try {
                if (lootBox.Inventory != null) {
                    foreach (Item item in lootBox.Inventory) {
                        VTModifiersCoreV2.PatchItem(item, VTModifiersCoreV2.Sources.SCAV);
                    }
                }
                // ModBehaviour.VTMO.Log($"[LootBoxEventListener] 已获取lootbox");
            }
            catch (System.ArgumentNullException ex) {
                VTMO.Log($"[LootBoxEventListener] 处理战利品箱修改事件时遇到空引用: {ex.Message}");
            }
            catch (System.InvalidOperationException ex) {
                VTMO.Log($"[LootBoxEventListener] 处理战利品箱修改事件时遇到操作异常: {ex.Message}");
            }
            catch (Exception ex) {
                VTMO.Log($"[LootBoxEventListener] 处理战利品箱修改事件时出错: {ex.Message}");
            }
        }
    }
}
