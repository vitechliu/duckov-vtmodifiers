using ItemStatsSystem;
using UnityEngine;
using System.Reflection;

namespace VTModifiers;

public class ModBehaviour : Duckov.Modding.ModBehaviour {
    protected string _logFilePath;
    protected string _dllDirectory;

    protected string _modName = "ModTest2";

    protected string _version = "0.0.1";
    protected bool _isInitialized = false;

    protected override void OnAfterSetup() {
        if (!_isInitialized) {
            _dllDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            InitializeLogFile();
            RegisterEvents();
            _isInitialized = true;
        }
    }

    protected override void OnBeforeDeactivate() {
        if (_isInitialized) {
            Log("模组已卸载");
            UnregisterEvents();
            _isInitialized = false;
        }
    }

    protected void RegisterEvents() {
        
    }

    protected void UnregisterEvents() {
    }

    // void Awake() {
    //     Debug.Log("fffff loaded!! v1");
    // }

    protected void InitializeLogFile() {
        string str = Path.Combine(this._dllDirectory, "logs");
        Directory.CreateDirectory(str);
        _logFilePath = Path.Combine(str,
            string.Format("{0}_log_{1:yyyyMMdd_HHmmss}.txt", this._modName, (object)DateTime.Now));
        Log("日志路径: " + this._logFilePath);
        Log("模组启动，开始初始化，版本:" + this._version);
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