using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bralaia
{
    [Serializable]
    public sealed class RegionData
    {
        public string Id;
        public string Name;
        public string Owner;
        public int Population;
        public int Food;
        public int Development;
        public int Army;
        public Vector2Int Grid;
        public readonly List<string> Neighbors = new List<string>();
        public bool IsOwnedBy(string faction) => Owner == faction;
    }

    [Serializable]
    internal sealed class RegionSaveData
    {
        public string Id, Owner;
        public int Population, Food, Development, Army;
    }

    [Serializable]
    internal sealed class WorldSaveData
    {
        public int Version = 1;
        public int Turn, Treasury, Manpower, Stability, TotalFood;
        public List<RegionSaveData> Regions = new List<RegionSaveData>();
        public List<string> Wars = new List<string>();
        public List<string> EventLog = new List<string>();
    }

    /// <summary>Lightweight strategy simulation state for one campaign.</summary>
    public sealed class BralaiaWorld
    {
        public const string PlayerFaction = "مملكة الفجر";
        public readonly List<RegionData> Regions = new List<RegionData>();
        public readonly List<string> EventLog = new List<string>();
        public int Turn { get; private set; } = 1;
        public int Treasury { get; private set; } = 500;
        public int Manpower { get; private set; } = 350;
        public int Stability { get; private set; } = 70;
        public int TotalFood { get; private set; } = 300;

        private readonly string[] aiFactions = { "اتحاد السهوب", "ممالك الساحل" };
        private readonly HashSet<string> wars = new HashSet<string>();

        public BralaiaWorld()
        {
            Add("north", "مرتفعات أزورا", "اتحاد السهوب", 84, 95, 2, 45, 0, 1);
            Add("northwest", "غابة إيلمار", PlayerFaction, 112, 115, 3, 80, -1, 0);
            Add("center", "سهول برالايا", PlayerFaction, 190, 160, 4, 120, 0, 0);
            Add("northeast", "وادي النهرين", "اتحاد السهوب", 105, 110, 3, 65, 1, 0);
            Add("west", "هضبة الرماد", PlayerFaction, 76, 80, 2, 55, -1, -1);
            Add("east", "ساحل اللؤلؤ", "ممالك الساحل", 132, 145, 3, 90, 1, -1);
            Add("southwest", "صحراء ميراث", PlayerFaction, 68, 65, 1, 40, -1, -2);
            Add("south", "بحيرة أورين", "ممالك الساحل", 98, 120, 2, 60, 0, -2);
            Add("southeast", "أراضي الظلال", "مستقلة", 58, 55, 1, 25, 1, -2);

            Link("northwest", "north"); Link("northwest", "center");
            Link("center", "north"); Link("center", "northeast");
            Link("center", "west"); Link("center", "east");
            Link("west", "southwest"); Link("west", "south");
            Link("east", "south"); Link("east", "southeast");
            Link("south", "southwest"); Link("south", "southeast");
            Log("بدأت حملتك في قلب برالايا. اختر طريقك نحو القوة.");
        }

        public RegionData GetRegion(string id) => Regions.Find(r => r.Id == id);
        public int CountOwned(string faction) => Regions.FindAll(r => r.Owner == faction).Count;
        public bool IsAtWar(string faction) => wars.Contains(WarKey(PlayerFaction, faction));

        public void Recruit(RegionData region)
        {
            if (!CanActOn(region)) return;
            if (Treasury < 80 || Manpower < 40) { Log("الموارد غير كافية للتجنيد."); return; }
            Treasury -= 80; Manpower -= 40; region.Army += 40;
            Log("تم تدريب 40 جنديًا في " + region.Name + ".");
        }

        public void DevelopFarms(RegionData region)
        {
            if (!CanActOn(region)) return;
            if (Treasury < 100) { Log("تحتاج إلى 100 ذهب لتطوير المزارع."); return; }
            Treasury -= 100; region.Development++; region.Food += 35; TotalFood += 35;
            Log("تطورت الزراعة في " + region.Name + "، وازداد مخزون الطعام.");
        }

        public void CollectTaxes()
        {
            int owned = CountOwned(PlayerFaction);
            if (owned == 0) { Log("لم تعد تملك مناطق لجباية الضرائب."); return; }
            Treasury += owned * 45; Stability = Mathf.Max(0, Stability - 7);
            Log("جُمعت الضرائب من " + owned + " مناطق؛ انخفض الاستقرار.");
        }

        public void DeclareWar(RegionData target)
        {
            if (target == null || target.Owner == PlayerFaction || target.Owner == "مستقلة") { Log("لا يمكن إعلان الحرب على هذه المنطقة."); return; }
            string key = WarKey(PlayerFaction, target.Owner);
            if (wars.Add(key)) Log("أعلنت ��ملكة الفجر الحرب على " + target.Owner + ".");
            else Log("الحرب قائمة بالفعل مع " + target.Owner + ".");
        }

        public void Attack(RegionData target)
        {
            if (target == null || target.Owner == PlayerFaction || target.Owner == "مستقلة") { Log("اختر منطقة معادية صالحة للهجوم."); return; }
            if (!IsAtWar(target.Owner)) { Log("أعلن الحرب أولًا قبل الهجوم."); return; }
            RegionData source = null;
            foreach (var neighborId in target.Neighbors)
            {
                var candidate = GetRegion(neighborId);
                if (candidate != null && candidate.Owner == PlayerFaction && (source == null || candidate.Army > source.Army)) source = candidate;
            }
            if (source == null) { Log("لا توجد منطقة لك مجاورة لهذا الهدف."); return; }
            if (source.Army < 20) { Log("تحتاج إلى 20 جنديًا على الأقل في " + source.Name + "."); return; }

            int committed = Mathf.Max(20, source.Army * 2 / 3);
            source.Army -= committed;
            int attackPower = committed + UnityEngine.Random.Range(0, 31);
            int defensePower = target.Army + Mathf.Max(5, target.Development * 4) + UnityEngine.Random.Range(0, 31);
            if (attackPower > defensePower)
            {
                target.Owner = PlayerFaction;
                target.Army = Mathf.Max(5, committed / 3);
                Stability = Mathf.Max(0, Stability - 3);
                Log("انتصرت قواتك وسيطرت على " + target.Name + "!");
            }
            else
            {
                target.Army = Mathf.Max(0, target.Army - committed / 4);
                Stability = Mathf.Max(0, Stability - 2);
                Log("فشل الهجوم على " + target.Name + " وتكبد جيشك خسائر.");
            }
        }

        public void OfferPeace(string faction)
        {
            string key = WarKey(PlayerFaction, faction);
            if (!wars.Contains(key)) { Log("لا توجد حرب مع " + faction + "."); return; }
            int playerArmy = 0, enemyArmy = 0;
            foreach (var region in Regions)
            {
                if (region.Owner == PlayerFaction) playerArmy += region.Army;
                if (region.Owner == faction) enemyArmy += region.Army;
            }
            if (playerArmy >= enemyArmy * 3 / 4)
            {
                wars.Remove(key);
                Log("قبلت " + faction + " السلام. انتهت الحرب.");
            }
            else Log("رفضت " + faction + " عرض السلام؛ عزز موقفك أولًا.");
        }

        public void NextTurn()
        {
            Turn++;
            int owned = 0, foodProduced = 0, population = 0;
            foreach (var region in Regions)
            {
                if (region.Owner != PlayerFaction) continue;
                owned++; population += region.Population;
                Treasury += 12 + region.Development * 3;
                foodProduced += 10 + region.Development * 5;
            }
            TotalFood += foodProduced - Mathf.Max(1, population / 45);
            if (TotalFood < 0)
            {
                Stability = Mathf.Max(0, Stability - 5); TotalFood = 0;
                Log("مجاعة تضرب أراضيك! طوّر الزراعة أو استولِ على مخازن جديدة.");
            }
            else TotalFood = Mathf.Min(9999, TotalFood);

            foreach (var faction in aiFactions) RunAiTurn(faction);
            if (Stability < 100 && Turn % 3 == 0) Stability++;
            Log("حلّ الدور " + Turn + ". دخل المناطق: " + owned + "، الطعام المنتج: " + foodProduced + ".");
        }

        public string SaveToJson()
        {
            var data = new WorldSaveData { Turn = Turn, Treasury = Treasury, Manpower = Manpower, Stability = Stability, TotalFood = TotalFood };
            foreach (var region in Regions)
                data.Regions.Add(new RegionSaveData { Id = region.Id, Owner = region.Owner, Population = region.Population, Food = region.Food, Development = region.Development, Army = region.Army });
            data.Wars.AddRange(wars);
            data.EventLog.AddRange(EventLog);
            return JsonUtility.ToJson(data);
        }

        public bool LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var data = JsonUtility.FromJson<WorldSaveData>(json);
                if (data == null || data.Version != 1 || data.Regions == null || data.Regions.Count != Regions.Count) return false;
                foreach (var saved in data.Regions)
                {
                    var region = GetRegion(saved.Id);
                    if (region == null || string.IsNullOrEmpty(saved.Owner)) return false;
                }
                foreach (var saved in data.Regions)
                {
                    var region = GetRegion(saved.Id);
                    region.Owner = saved.Owner; region.Population = saved.Population; region.Food = saved.Food;
                    region.Development = saved.Development; region.Army = Mathf.Max(0, saved.Army);
                }
                Turn = Mathf.Max(1, data.Turn); Treasury = Mathf.Max(0, data.Treasury);
                Manpower = Mathf.Max(0, data.Manpower); Stability = Mathf.Clamp(data.Stability, 0, 100);
                TotalFood = Mathf.Max(0, data.TotalFood);
                wars.Clear(); if (data.Wars != null) foreach (var war in data.Wars) if (!string.IsNullOrEmpty(war)) wars.Add(war);
                EventLog.Clear(); if (data.EventLog != null) foreach (var message in data.EventLog) Log(message);
                Log("تم تحميل الحملة المحفوظة.");
                return true;
            }
            catch (Exception) { return false; }
        }

        private void RunAiTurn(string faction)
        {
            var owned = Regions.FindAll(r => r.Owner == faction);
            if (owned.Count == 0) return;
            foreach (var region in owned)
            {
                foreach (var neighborId in region.Neighbors)
                {
                    var neighbor = GetRegion(neighborId);
                    if (neighbor != null && neighbor.Owner == "مستقلة")
                    {
                        neighbor.Owner = faction; neighbor.Army += 8;
                        Log(faction + " بسط نفوذه على " + neighbor.Name + ".");
                        return;
                    }
                }
            }
            var strongest = owned[0];
            foreach (var region in owned) if (region.Army > strongest.Army) strongest = region;
            strongest.Army += 5;
        }

        private bool CanActOn(RegionData region)
        {
            if (region != null && region.Owner == PlayerFaction) return true;
            Log("اختر منطقة تابعة لك أولًا."); return false;
        }
        private string WarKey(string first, string second) => string.CompareOrdinal(first, second) < 0 ? first + "|" + second : second + "|" + first;
        private void Add(string id, string name, string owner, int population, int food, int development, int army, int x, int y)
        {
            Regions.Add(new RegionData { Id = id, Name = name, Owner = owner, Population = population, Food = food, Development = development, Army = army, Grid = new Vector2Int(x, y) });
        }
        private void Link(string first, string second) { GetRegion(first).Neighbors.Add(second); GetRegion(second).Neighbors.Add(first); }
        private void Log(string message) { EventLog.Insert(0, message); if (EventLog.Count > 8) EventLog.RemoveAt(EventLog.Count - 1); }
    }
}
