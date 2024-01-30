using HarmonyLib;
using SRML;
using SRML.SR;
using SRML.Console;
using Console = SRML.Console.Console;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using SRML.Utils.Enum;
using System.Linq;

namespace UpgradeableDroneProgramming
{
    public class Main : ModEntryPoint
    {
        internal static Assembly modAssembly = Assembly.GetExecutingAssembly();
        internal static string modName = $"{modAssembly.GetName().Name}";
        internal static string modDir = $"{Environment.CurrentDirectory}\\SRML\\Mods\\{modName}";
        internal static Sprite anyFoodIcon = LoadImage("any_food_icon.png", 525, 525).CreateSprite();
        internal static Sprite anyIcon = LoadImage("any_icon.png", 1024, 1024).CreateSprite();
        internal static Sprite freerangeNCollectorIcon = LoadImage("freerange_collector_icon.png", 525, 525).CreateSprite();
        public static bool droneUpgrade => SceneContext.Instance.PlayerState.HasUpgrade(Ids.DRONE_PROGRAMMING);
        internal static Identifiable.Id FoodId = (Identifiable.Id)(-1);
        internal static List<DroneMetadata.Program.Target> customTargets = new List<DroneMetadata.Program.Target>()
        {
            new DroneMetadata.Program.Target()
                {
                    id = "m.drone.target.name.category_food",
                    image = anyFoodIcon,
                    ident = FoodId,
                    predicate = (x) => Identifiable.IsFood(x)
                }
        };
        internal static List<DroneMetadata.Program.Behaviour> customSources = new List<DroneMetadata.Program.Behaviour>()
        {
            new DroneMetadata.Program.Behaviour()
                {
                    id = "m.drone.source.name.freerange_n_collector",
                    image = freerangeNCollectorIcon,
                    isCompatible = (x) => droneUpgrade && Identifiable.IsPlort(x.target.ident),
                    types = new Type[]
                    {
                        typeof(DroneProgramSourceFreeRange),
                        typeof(DroneProgramSourcePlortCollector)
                    }
                }
        };
        public static int VIKTOR_REWARD_EXPANSION = Traverse.Create<ProgressDirector>().Field("VIKTOR_REWARD_EXPANSION").GetValue<int>();

        public override void PreLoad()
        {
            HarmonyInstance.PatchAll();
            LookupRegistry.RegisterUpgradeEntry(Ids.DRONE_PROGRAMMING.Define(Resources.FindObjectsOfTypeAll<Sprite>().First((x) => x.name == "iconDroneSource"), 2500));
            PersonalUpgradeRegistry.RegisterUpgradeLock(
                Ids.DRONE_PROGRAMMING,
                (x) => new PlayerState.UpgradeLocker(
                    x,
                    () => SceneContext.Instance.ProgressDirector.model != null
                        && SceneContext.Instance.ProgressDirector.GetProgress(ProgressDirector.ProgressType.VIKTOR_REWARDS) >= VIKTOR_REWARD_EXPANSION
                         && SceneContext.Instance.ProgressDirector.HasProgress(ProgressDirector.ProgressType.EXCHANGE_BOB),
                    12
                )
            );
        }
        public override void PostLoad()
        {
            customSources.Add(new DroneMetadata.Program.Behaviour()
            {
                id = "m.drone.source.name.anywhere",
                image = anyIcon,
                isCompatible = (x) => droneUpgrade,
                types = DroneRegistry.GetMetadatas().GetAll((x) => x.sources.GetAll((y) => y.types)).ToArray()
            });
            foreach (DroneMetadata droneMetadata in DroneRegistry.GetMetadatas())
            {
                var newTargets = droneMetadata.targets.ToList();
                if (newTargets.Exists((x) => Identifiable.IsFood(x.ident)))
                    newTargets.Insert(0, customTargets[0]);
                droneMetadata.targets = newTargets.ToArray();
                var newSources = droneMetadata.sources.ToList();
                foreach (var freeRangeSource in newSources.FindAll((x) => x.types.Contains(typeof(DroneProgramSourceFreeRange))))
                {
                    var pred = freeRangeSource.isCompatible;
                    freeRangeSource.isCompatible = (x) => droneUpgrade || pred(x);
                }
                var ind = newSources.FindIndex((x) => x.types.Contains(typeof(DroneProgramSourceFreeRange)) || x.types.Contains(typeof(DroneProgramSourcePlortCollector)));
                if (ind != -1)
                    newSources.Insert(ind, customSources[0]);
                newSources.Insert(0, customSources[1]);
                droneMetadata.sources = newSources.ToArray();
            }
            GameContext.Instance.MessageDirector.RegisterBundlesListener(b =>
            {
                var dict = b.GetBundle("ui").bundle.dict;
                var lang = b.GetCultureLang();
                if (lang == MessageDirector.Lang.EN)
                    dict["m.drone.target.name.category_food"] = "Food";
                else if (lang == MessageDirector.Lang.DE)
                    dict["m.drone.target.name.category_food"] = "Nahrungs";
                else if (lang == MessageDirector.Lang.ES)
                    dict["m.drone.target.name.category_food"] = "Comida";
                else if (lang == MessageDirector.Lang.FR)
                    dict["m.drone.target.name.category_food"] = "Aliment";
                else if (lang == MessageDirector.Lang.RU)
                    dict["m.drone.target.name.category_food"] = "Еды";
                else if (lang == MessageDirector.Lang.SV)
                    dict["m.drone.target.name.category_food"] = "Mat";
                else if (lang == MessageDirector.Lang.ZH)
                    dict["m.drone.target.name.category_food"] = "食物";
                else if (lang == MessageDirector.Lang.JA)
                    dict["m.drone.target.name.category_food"] = "エサ";
                else if (lang == MessageDirector.Lang.PT)
                    dict["m.drone.target.name.category_food"] = "Comida";
                else if (lang == MessageDirector.Lang.KO)
                    dict["m.drone.target.name.category_food"] = "음식";
                dict["m.drone.source.name.freerange_n_collector"] = dict["m.drone.source.name.free_range"] + " / " + dict["m.drone.source.name.plort_collector"];
                dict = b.GetBundle("pedia").bundle.dict;
                if (lang == MessageDirector.Lang.EN)
                    dict["m.upgrade.name.personal.drone_programming"] = "Drone Programming";
                else if (lang == MessageDirector.Lang.DE)
                    dict["m.upgrade.name.personal.drone_programming"] = "Drohnen-Programmierung";
                else if (lang == MessageDirector.Lang.ES)
                    dict["m.upgrade.name.personal.drone_programming"] = "Programación de dron";
                else if (lang == MessageDirector.Lang.FR)
                    dict["m.upgrade.name.personal.drone_programming"] = "La programmation de drone";
                else if (lang == MessageDirector.Lang.RU)
                    dict["m.upgrade.name.personal.drone_programming"] = "Программирование дрона";
                else if (lang == MessageDirector.Lang.SV)
                    dict["m.upgrade.name.personal.drone_programming"] = "Drönarprogrammering";
                else if (lang == MessageDirector.Lang.ZH)
                    dict["m.upgrade.name.personal.drone_programming"] = "无人机编程";
                else if (lang == MessageDirector.Lang.JA)
                    dict["m.upgrade.name.personal.drone_programming"] = "ドローンプログラミング";
                else if (lang == MessageDirector.Lang.PT)
                    dict["m.upgrade.name.personal.drone_programming"] = "Programação Drone";
                else if (lang == MessageDirector.Lang.KO)
                    dict["m.upgrade.name.personal.drone_programming"] = "드론 프로그램 작성";
                dict["m.upgrade.desc.personal.drone_programming"] = "Improves your ability to program drones";
            });
        }

        public static void CheckUpgradeUnlock()
        {
            if (
                SceneContext.Instance 
                && SceneContext.Instance.PlayerState
                && SceneContext.Instance.PlayerState.model != null
                && SceneContext.Instance.PlayerState.model.upgradeLocks != null
                && SceneContext.Instance.PlayerState.model.upgradeLocks.TryGetValue(Ids.DRONE_PROGRAMMING, out var upgrade)
                && upgrade.CheckUnlockCondition())
            {
                upgrade.Unlock();
            }
        }
        public static void Log(string message) => Console.Log($"[{modName}]: " + message);
        public static void LogError(string message) => Console.LogError($"[{modName}]: " + message);
        public static void LogWarning(string message) => Console.LogWarning($"[{modName}]: " + message);
        public static void LogSuccess(string message) => Console.LogSuccess($"[{modName}]: " + message);

        public static Texture2D LoadImage(string filename, int width, int height)
        {
            var a = modAssembly;
            var spriteData = a.GetManifestResourceStream(a.GetName().Name + "." + filename);
            var rawData = new byte[spriteData.Length];
            spriteData.Read(rawData, 0, rawData.Length);
            var tex = new Texture2D(width, height);
            tex.LoadImage(rawData);
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }
    }

    [EnumHolder]
    static class Ids
    {
        public static PlayerState.Upgrade DRONE_PROGRAMMING;
    }
    public static class ExtentionMethods
    {
        public static Sprite CreateSprite(this Texture2D texture) => Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1);

        public static List<Y> GetAll<X,Y>(this IEnumerable<X> os, Func<X, IEnumerable<Y>> collector, bool ignoreDuplicates = true)
        {
            var l = new List<Y>();
            foreach (var o in os) {
                var c = collector(o);
                if (c != null)
                {
                    if (ignoreDuplicates)
                        l.AddRangeUnique(c);
                    else
                        l.AddRange(c);
                }
            }
            return l;
        }

        public static void AddRangeUnique<X>(this List<X> c, IEnumerable<X> collection)
        {
            foreach (var i in collection)
                if (!c.Contains(i))
                    c.Add(i);
        }

        public static UpgradeDefinition Define(this PlayerState.Upgrade upgrade, Sprite sprite, int cost)
        {
            var o = ScriptableObject.CreateInstance<UpgradeDefinition>();
            o.upgrade = upgrade;
            o.icon = sprite;
            o.cost = cost;
            return o;
        }
    }

    [HarmonyPatch(typeof(DroneUI), "GatherTarget")]
    class Patch_DroneTargetUI
    {
        static void Prefix(DroneUI __instance, ref DroneMetadata.Program.Target[] __state)
        {
            if (!Main.droneUpgrade)
            {
                __state = __instance.metadata.targets;
                var newTargets = __instance.metadata.targets.ToList();
                newTargets.RemoveAll((x) => Main.customTargets.Contains(x));
                __instance.metadata.targets = newTargets.ToArray();
            }
        }
        static void Postfix(DroneUI __instance, DroneMetadata.Program.Target[] __state)
        {
            if (__state != null)
                __instance.metadata.targets = __state;
        }
    }

    [HarmonyPatch(typeof(DroneGadget), "SetPrograms")]
    class Patch_SetDronePrograms
    {
        static void Prefix(DroneGadget __instance, DroneMetadata.Program[] programs)
        {
            foreach (var p in programs)
            {
                if (p.source.id == "m.drone.source.name.anywhere")
                {
                    var exclude = __instance.metadata.sources.GetAll((x) => x.id == p.destination.id.Replace("m.drone.destination.", "m.drone.source.") ? x.types : null);
                    p.source = new DroneMetadata.Program.Behaviour()
                    {
                        id = p.source.id,
                        image = p.source.image,
                        isCompatible = p.source.isCompatible,
                        types = p.source.types.Except(__instance.metadata.sources.GetAll((x) => x.id == p.destination.id.Replace("m.drone.destination.", "m.drone.source.") ? x.types : null)).ToArray()
                    };
                }
            }
        }
    }

    [HarmonyPatch(typeof(Identifiable), "IsAnimal")]
    class Patch_Identifiable_IsAnimal
    {
        static void Postfix(Identifiable.Id id, ref bool __result)
        {
            if (id == Main.FoodId)
                __result = true;
        }
    }
    [HarmonyPatch(typeof(Identifiable), "IsFruit")]
    class Patch_Identifiable_IsFruit
    {
        static void Postfix(Identifiable.Id id, ref bool __result)
        {
            if (id == Main.FoodId)
                __result = true;
        }
    }
    [HarmonyPatch(typeof(Identifiable), "IsVeggie")]
    class Patch_Identifiable_IsVeggie
    {
        static void Postfix(Identifiable.Id id, ref bool __result)
        {
            if (id == Main.FoodId)
                __result = true;
        }
    }
    [HarmonyPatch(typeof(Identifiable), "IsFood")]
    class Patch_Identifiable_IsFood
    {
        static void Postfix(Identifiable.Id id, ref bool __result)
        {
            if (id == Main.FoodId)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(ProgressDirector), "SetModel")]
    class Patch_LoadProgress
    {
        static void Prefix(ProgressDirector __instance) => __instance.onProgressChanged += Main.CheckUpgradeUnlock;
    }
}