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

    /// <summary>Deterministic, lightweight simulation state for one strategic campaign.</summary>
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
        private readonly Dictionary<string, int> aiStrength = new Dictionary<string, int>();

        public BralaiaWorld()
        {
            Add("north", "مرتفعات أزورا", "اتحاد السهوب", 84, 95, 2, 45, 0, 1);
            Add("northwest", "غابة إيلمار", "مملكة الفجر", 112, 115, 3, 80, -1, 0);
            Add("center", "سهول برالايا", PlayerFaction, 190, 160, 4, 120, 0, 0);
            Add("northeast", "وادي النهرين", "اتحاد السهوب", 105, 110, 3, 65, 1, 0);
            Add("west", "هضبة الرماد", "مملكة الفجر", 76, 80, 2, 55, -1, -1);
            Add("east", "ساحل اللؤلؤ", "ممالك الساحل", 132, 145, 3, 90, 1, -1);
            Add("southwest", "صحراء ميراث", "مملكة الفجر", 68, 65, 1, 40, -1, -2);
            Add("south", "بحيرة أورين", "ممالك الساحل", 98, 120, 2, 60, 0, -2);
            Add("southeast", "أراضي الظلال", "مستقلة", 58, 55, 1, 25, 1, -2);

            Link("northwest", "north"); Link("northwest", "center");
            Link("center", "north"); Link("center", "northeast");
            Link("center", "west"); Link("center", "east");
            Link("west", "southwest"); Link("west", "south");
            Link("east", "south"); Link("east", "southeast");
            Link("south", "southwest"); Link("south", "southeast");
            aiStrength[aiFactions[0]] = 210;
            aiStrength[aiFactions[1]] = 190;
            Log("بدأت حملتك في قلب برالايا. اختر طريقك نحو القوة.");
        }

        public RegionData GetRegion(string id) => Regions.Find(r => r.Id == id);

        public int CountOwned(string faction)
        {
            int count = 0;
            foreach (var region in Regions) if (region.Owner == faction) count++;
            return count;
        }

        public void Recruit(RegionData region)
        {
            if (!CanActOn(region)) return;
            if (Treasury < 80 || Manpower < 40) { Log("الموارد غير كافية للتجنيد."); return; }
            Treasury -= 80;
            Manpower -= 40;
            region.Army += 40;
            Log("تم تدريب 40 جنديًا في " + region.Name + ".");
        }

        public void DevelopFarms(RegionData region)
        {
            if (!CanActOn(region)) return;
            if (Treasury < 100) { Log("تحتاج إلى 100 ذهب لتطوير المزارع."); return; }
            Treasury -= 100;
            region.Development++;
            region.Food += 35;
            TotalFood += 35;
            Log("تطورت الزراعة في " + region.Name + "، وازداد مخزون الطعام.");
        }

        public void CollectTaxes()
        {
            int owned = CountOwned(PlayerFaction);
            if (owned == 0) { Log("لم تعد تملك مناطق لجباية الضرائب."); return; }
            Treasury += owned * 45;
            Stability = Mathf.Max(0, Stability - 7);
            Log("جُمعت الضرائب من " + owned + " مناطق؛ انخفض الاستقرار.");
        }

        public void NextTurn()
        {
            Turn++;
            int owned = 0, foodProduced = 0, population = 0;
            foreach (var region in Regions)
            {
                if (region.Owner != PlayerFaction) continue;
                owned++;
                population += region.Population;
                Treasury += 12 + region.Development * 3;
                foodProduced += 10 + region.Development * 5;
            }
            TotalFood += foodProduced - Mathf.Max(1, population / 45);
            if (TotalFood < 0)
            {
                Stability = Mathf.Max(0, Stability - 5);
                TotalFood = 0;
                Log("مجاعة تضرب أراضيك! طوّر الزراعة أو استولِ على مخازن جديدة.");
            }
            else if (TotalFood > 0) TotalFood = Mathf.Min(9999, TotalFood);

            foreach (var faction in aiFactions) RunAiTurn(faction);
            if (Stability < 100 && Turn % 3 == 0) Stability++;
            Log("حلّ الدور " + Turn + ". دخل المناطق: " + owned + "، الطعام المنتج: " + foodProduced + ".");
        }

        private void RunAiTurn(string faction)
        {
            var owned = Regions.FindAll(r => r.Owner == faction);
            if (owned.Count == 0) return;
            aiStrength[faction] += owned.Count * 5;

            // The neighboring powers prioritize open land; if none is available, they consolidate.
            foreach (var region in owned)
            {
                foreach (var neighborId in region.Neighbors)
                {
                    var neighbor = GetRegion(neighborId);
                    if (neighbor != null && neighbor.Owner == "مستقلة")
                    {
                        neighbor.Owner = faction;
                        neighbor.Army += 8;
                        aiStrength[faction] += 12;
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
            Log("اختر منطقة تابعة لك أولًا.");
            return false;
        }

        private void Add(string id, string name, string owner, int population, int food, int development, int army, int x, int y)
        {
            Regions.Add(new RegionData { Id = id, Name = name, Owner = owner, Population = population, Food = food, Development = development, Army = army, Grid = new Vector2Int(x, y) });
        }

        private void Link(string first, string second)
        {
            GetRegion(first).Neighbors.Add(second);
            GetRegion(second).Neighbors.Add(first);
        }

        private void Log(string message)
        {
            EventLog.Insert(0, message);
            if (EventLog.Count > 8) EventLog.RemoveAt(EventLog.Count - 1);
        }
    }
}
