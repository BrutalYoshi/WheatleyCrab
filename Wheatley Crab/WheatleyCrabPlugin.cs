using BepInEx;
using RoR2;
using System;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Wheatley_Crab
{
    [HarmonyPatch]
    [BepInPlugin("brutalyoshi.wheatleycrab", "Wheatley Crab", "1.0")]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    public class WheatleyCrabPlugin : BaseUnityPlugin
    {
        public static bool IsEnabledROO => Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions");
        private AssetBundle bundle;
        public static ConfigEntry<float> amplitude;
        public static ConfigEntry<float> speed;
        public static ConfigEntry<float> lower;
        public static ConfigEntry<float> upper;
        public static ConfigEntry<float> crabVolume;
        private static bool soundBankQueued;
        private static string pluginPath;

        public static CharacterBody worker;
        //ඞ
        public void Awake()
        {
            pluginPath = System.IO.Path.GetDirectoryName(Info.Location) ??
                         throw new InvalidOperationException("Failed to find path of plugin.");

            var bundlePath = System.IO.Path.Join(pluginPath, "wheatleycrap");
            AssetBundle.LoadFromFileAsync(bundlePath).completed += operation =>
            {
                bundle = ((AssetBundleCreateRequest)operation).assetBundle;
            };


            var harm = new Harmony(Info.Metadata.GUID);
            harm.PatchAll();

            RoR2Application.onLoad += Loaded;

            amplitude = Config.Bind("General", "Wobble Distance", 0.32f, "Maximum sway for Wheatley");
            speed = Config.Bind("General", "Wobble Speed", 12f, "Wobble Speed");
            lower = Config.Bind("General", "Huh Min time", 1f, "Minimum time in seconds for Wheatley to say \"huh\"");
            upper = Config.Bind("General", "Huh Max time", 2f, "Maximum time in seconds for Wheatley to say \"huh\"");
            crabVolume = Config.Bind("General", "Crab Volume", 50f, "Loudness of Crab.");

            crabVolume.SettingChanged += (_, _) => AkSoundEngine.SetRTPCValue("Crab_Volume", crabVolume.Value);
            AkSoundEngine.SetRTPCValue("Crab_Volume", crabVolume.Value);

            if (IsEnabledROO)
                InitRoo();
        }

        public void InitRoo()
        {
            ModSettingsManager.AddOption(new SliderOption(amplitude, new SliderConfig() {min = 0.1f, max = 2f, formatString = "{0:F2}"}));
            ModSettingsManager.AddOption(new SliderOption(speed, new SliderConfig() {min = 0.1f, max = 20f, formatString = "{0:F2}"}));
            ModSettingsManager.AddOption(new SliderOption(lower, new SliderConfig() {min = 0.1f, max = 5f, formatString = "{0:F2}"}));
            ModSettingsManager.AddOption(new SliderOption(upper, new SliderConfig() {min = 0.1f, max = 5f, formatString = "{0:F2}"}));
            ModSettingsManager.AddOption(new SliderOption(crabVolume, new SliderConfig() {min = 0f, max = 100f, formatString = "{0:F2}"}));
        }

        private void Loaded()
        {
            Language.english.SetStringByToken("WHEATLEY_CRAB_NAME","WHEATLEY Crab");
            Language.english.SetStringByToken("WHEATLEY_CRAB_SUBTITLE","heh");

            worker = DLC3Content.BodyPrefabs.WorkerUnitBody;
            var modelTransform = worker.gameObject.transform.Find("ModelBase/mdlWorkerUnit");
            DestroyImmediate(modelTransform.Find("meshWorkerUnit").gameObject);
            var crab = bundle.LoadAsset<GameObject>("WheatleyCrabheh");

            crab.transform.SetParent(modelTransform);
            crab.transform.localScale = new Vector3(80, 80, 80);
            crab.transform.localPosition = Vector3.zero;
            modelTransform.gameObject.AddComponent<CrabMovement>();
            worker.gameObject.AddComponent<CrabNoises>();

            worker.baseNameToken = "WHEATLEY_CRAB_NAME";
            worker.subtitleNameToken = "WHEATLEY_CRAB_SUBTITLE";
            worker.portraitIcon = bundle.LoadAsset<Sprite>("texWorkerUnitIcon").texture;
/*
crab.transform.Find("Armature/mdlWorkerUnit/wheatley_crab_reference").SetParent(modelTransform);
crab.transform.Find("meshWorkerUnit.040").SetParent(modelTransform);
DestroyImmediate(crab);
*/
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(RoR2.WwiseUtils.SoundbankLoader), nameof(RoR2.WwiseUtils.SoundbankLoader.Start))]
        public static void AddSoundbankToLoader(RoR2.WwiseUtils.SoundbankLoader __instance)
        {
            if (soundBankQueued) return;
            AkSoundEngine.AddBasePath(pluginPath);
            __instance.soundbankStrings = __instance.soundbankStrings
                .AddItem("Wheat.bnk").ToArray();
            soundBankQueued = true;
        }
    }

    public class CrabMovement : MonoBehaviour
    {
        private Transform crab;

        public void Awake()
        {
            crab = transform.Find("WheatleyCrabheh");
        }

        public void Update()
        {
            crab.localPosition = new Vector3(Mathf.Sin(Time.time * WheatleyCrabPlugin.speed.Value) * WheatleyCrabPlugin.amplitude.Value, 0, 0);
        }
    }

    public class CrabNoises : MonoBehaviour
    {
        public void Awake()
        {
            GetComponent<SfxLocator>().deathSound = "crabsound";
        }

        public float nextHuh;
        public void FixedUpdate()
        {
            if (nextHuh > 0)
            {
                nextHuh -= Time.fixedDeltaTime;
            }
            else
            {
                nextHuh = Random.Range(WheatleyCrabPlugin.lower.Value, WheatleyCrabPlugin.upper.Value);
                Util.PlaySound("Crabrandom", gameObject);
            }
        }
    }
}