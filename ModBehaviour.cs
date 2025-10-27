using ItemStatsSystem;
using UnityEngine;
using System.Reflection;
using HarmonyLib;
using Unity.VisualScripting;

namespace VTModifiers;

[HarmonyPatch]
public class ModBehaviour : Duckov.Modding.ModBehaviour {
    protected string _logFilePath;
    protected string _dllDirectory;

    protected string _modName = "ModTest2";

    protected string _version = "0.0.1";
    protected bool _isInitialized = false;

    private static ModBehaviour _instance;
    public static ModBehaviour Instance => _instance;

    private Harmony _harmony;

    protected virtual void Awake() {
        if (_instance == null) {
            _instance = this;
            DontDestroyOnLoad(gameObject); // 确保单例在场景切换时不会销毁
        }
        else if (_instance != this) {
            Destroy(gameObject); // 销毁多余的实例
        }
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemAgent_Gun), "ShootOneBullet")]
    public static void ItemAgentGun_ShootOneBullet_PostFix(ItemAgent_Gun __instance) {
        Projectile temp = Traverse.Create(__instance).Field("projInst").GetValue<Projectile>();
        float beforeDamage = temp.context.damage;
        LogStatic($"BeforeDamage:{beforeDamage}");
        temp.context.damage += 10f;
        Traverse.Create(__instance).Field("projInst").SetValue(temp);
    }

    protected override void OnAfterSetup() {
        if (!_isInitialized) {
            _dllDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            InitializeLogFile();
            _harmony = new Harmony("com.vitech.duckov_vt_modifiers_patch");
            _harmony.PatchAll();
            RegisterEvents();
            _isInitialized = true;
        }
    }

    protected override void OnBeforeDeactivate() {
        if (_isInitialized) {
            Log("模组已卸载");
            _harmony.UnpatchAll();
            UnregisterEvents();
            _isInitialized = false;
        }
    }

    protected void RegisterEvents() { }

    protected void UnregisterEvents() { }

    // void Awake() {
    //     Debug.Log("fffff loaded!! v1");
    // }

    protected void InitializeLogFile() {
        string str = Path.Combine(this._dllDirectory, "logs");
        Directory.CreateDirectory(str);
        _logFilePath = Path.Combine(str,
            string.Format("{0}_log_{1:yyyyMMdd}.txt", this._modName, (object)DateTime.Now));
        Log("日志路径: " + _logFilePath);
        Log("模组启动，开始初始化，版本:" + _version);
    }

    public static void LogStatic(string message, bool isError = false) {
        if (ModBehaviour.Instance) {
            ModBehaviour.Instance.Log(message, isError);
        }
    }
    protected void Log(string message, bool isError = false) {
        try {
            File.AppendAllText(this._logFilePath,
                string.Format("[{0:HH:mm:ss}] {1}\n", (object)DateTime.Now, (object)message));
            if (isError) Debug.LogError((object)("[" + this._modName + "]" + message));
            else Debug.Log((object)("[" + this._modName + "]" + message));
        }
        catch (Exception ex) {
            Debug.LogError((object)("日志写入失败: " + ex.Message));
        }
    }
}