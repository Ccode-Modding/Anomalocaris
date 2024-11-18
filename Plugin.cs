using BepInEx;
using BepInEx.Logging;
using LethalLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using static LethalLib.Modules.Enemies;

namespace Anomalocaris
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "com.ccode.anomalocaris";
        public const string NAME = "Anomalocaris";
        public const string VERSION = "0.0.1";

        public static ManualLogSource Log;

        public static EnemyType AnomalocarisEnemy;

        public void Awake()
        {
            Assets.PopulateAssets();

            AnomalocarisEnemy = Assets.MainAssetBundle.LoadAsset<EnemyType>("AnomalocarisEnemy");
            var Node = Assets.MainAssetBundle.LoadAsset<TerminalNode>("AnomalocarisTN");
            var Keyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("AnomalocarisKW");

            RegisterEnemy(AnomalocarisEnemy, 999, LethalLib.Modules.Levels.LevelTypes.All, SpawnType.Daytime, Node, Keyword);
            Log = Logger;
            Log.LogInfo(NAME + " mod version " + VERSION + " loaded.");

            // netcode patcher
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }

    public static class Assets
    {
        public static AssetBundle MainAssetBundle = null;
        public static void PopulateAssets()
        {
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            MainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "anomalocarisassets"));
            if (MainAssetBundle == null)
            {
                Plugin.Log.LogError("Failed to load custom assets.");
                return;
            }
        }
    }
}
