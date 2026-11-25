using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bralaia
{
    [Serializable]
    public sealed class CourtAdvisor
    {
        public string Id;
        public string Name;
        public string Office;
        public int Loyalty;
        public int Influence;
        public int Relationship;
        public string Assignment;
    }

    [Serializable]
    internal sealed class CourtSaveData
    {
        public int Version = 1;
        public int CampaignTurn = 1;
        public List<CourtAdvisor> Advisors = new List<CourtAdvisor>();
        public List<string> EventLog = new List<string>();
    }

    /// <summary>
    /// A deterministic, local court simulation. It deliberately keeps its state separate
    /// from the campaign save so older campaign saves remain compatible.
    /// </summary>
    public sealed class BralaiaCourt
    {
        private const int SaveVersion = 1;
        private const int MaxLogEntries = 8;

        public readonly List<CourtAdvisor> Advisors = new List<CourtAdvisor>();
        public readonly List<string> EventLog = new List<string>();
        public int CampaignTurn { get; private set; } = 1;

        public BralaiaCourt()
        {
            Advisors.Add(new CourtAdvisor
            {
                Id = "vizier",
                Name = "ليان",
                Office = "الوزيرة",
                Loyalty = 72,
                Influence = 65,
                Relationship = 55,
                Assignment = "إدارة شؤون المملكة"
            });
            Advisors.Add(new CourtAdvisor
            {
                Id = "marshal",
                Name = "راشد",
                Office = "قائد الحرس",
                Loyalty = 64,
                Influence = 58,
                Relationship = 50,
                Assignment = "حراسة الحدود"
            });
            Advisors.Add(new CourtAdvisor
            {
                Id = "steward",
                Name = "مريم",
                Office = "أمينة الخزانة",
                Loyalty = 68,
                Influence = 61,
                Relationship = 60,
                Assignment = "مراجعة المخازن"
            });

            AddEvent("اجتمع مجلس المملكة لأول مرة، وكل مستشار ينتظر توجيهك.");
        }

        public CourtAdvisor GetAdvisor(string id)
        {
            return Advisors.Find(advisor => advisor.Id == id);
        }

        /// <summary>Advance court state once for each new campaign turn.</summary>
        public bool AdvanceTurn(int campaignTurn)
        {
            if (campaignTurn <= CampaignTurn)
                return false;

            CampaignTurn = campaignTurn;
            if (campaignTurn % 3 == 0)
                ResolveCouncilEvent(campaignTurn);
            else
                AddEvent("استقر المجلس في الدور " + campaignTurn + "، واستمر المستشارون في مهامهم.");

            return true;
        }

        /// <summary>Spend the turn's council attention to improve one advisor relationship.</summary>
        public bool HoldAudience(string advisorId)
        {
            var advisor = GetAdvisor(advisorId);
            if (advisor == null)
                return false;

            advisor.Relationship = Mathf.Clamp(advisor.Relationship + 8, 0, 100);
            advisor.Loyalty = Mathf.Clamp(advisor.Loyalty + 2, 0, 100);
            AddEvent("عقدتَ جلسة مع " + advisor.Name + "؛ تحسنت العلاقة والثقة.");
            return true;
        }

        /// <summary>Assign a role-appropriate project to an advisor.</summary>
        public bool AssignProject(string advisorId)
        {
            var advisor = GetAdvisor(advisorId);
            if (advisor == null)
                return false;

            if (advisor.Id == "vizier") advisor.Assignment = "التفاوض مع زعماء المناطق";
            else if (advisor.Id == "marshal") advisor.Assignment = "تدريب حرس الحدود";
            else if (advisor.Id == "steward") advisor.Assignment = "تحسين إدارة المخازن";
            else return false;

            advisor.Influence = Mathf.Clamp(advisor.Influence + 3, 0, 100);
            AddEvent("كُلّف " + advisor.Name + " بمهمة: " + advisor.Assignment + ".");
            return true;
        }

        public string SaveToJson()
        {
            var data = new CourtSaveData { CampaignTurn = CampaignTurn };
            foreach (var advisor in Advisors)
            {
                data.Advisors.Add(new CourtAdvisor
                {
                    Id = advisor.Id,
                    Name = advisor.Name,
                    Office = advisor.Office,
                    Loyalty = advisor.Loyalty,
                    Influence = advisor.Influence,
                    Relationship = advisor.Relationship,
                    Assignment = advisor.Assignment
                });
            }
            data.EventLog.AddRange(EventLog);
            return JsonUtility.ToJson(data);
        }

        /// <summary>Load only a complete, known advisor roster; invalid data changes nothing.</summary>
        public bool LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return false;

            CourtSaveData data;
            try
            {
                data = JsonUtility.FromJson<CourtSaveData>(json);
            }
            catch (Exception)
            {
                return false;
            }

            if (data == null || data.Version != SaveVersion || data.Advisors == null || data.Advisors.Count != Advisors.Count)
                return false;

            var seen = new HashSet<string>();
            foreach (var saved in data.Advisors)
            {
                var existing = GetAdvisor(saved.Id);
                if (existing == null || !seen.Add(saved.Id) || string.IsNullOrEmpty(saved.Name) ||
                    string.IsNullOrEmpty(saved.Office) || string.IsNullOrEmpty(saved.Assignment) ||
                    saved.Loyalty < 0 || saved.Loyalty > 100 ||
                    saved.Influence < 0 || saved.Influence > 100 ||
                    saved.Relationship < 0 || saved.Relationship > 100)
                    return false;
            }

            foreach (var saved in data.Advisors)
            {
                var advisor = GetAdvisor(saved.Id);
                advisor.Name = saved.Name;
                advisor.Office = saved.Office;
                advisor.Loyalty = saved.Loyalty;
                advisor.Influence = saved.Influence;
                advisor.Relationship = saved.Relationship;
                advisor.Assignment = saved.Assignment;
            }

            CampaignTurn = Mathf.Max(1, data.CampaignTurn);
            EventLog.Clear();
            if (data.EventLog != null)
            {
                int first = Mathf.Max(0, data.EventLog.Count - MaxLogEntries);
                for (int i = first; i < data.EventLog.Count; i++)
                    if (!string.IsNullOrEmpty(data.EventLog[i])) EventLog.Add(data.EventLog[i]);
            }
            return true;
        }

        private void ResolveCouncilEvent(int turn)
        {
            int eventIndex = (turn / 3) % 3;
            if (eventIndex == 0)
            {
                var advisor = GetAdvisor("vizier");
                advisor.Relationship = Mathf.Clamp(advisor.Relationship + 4, 0, 100);
                AddEvent("أثمرت مفاوضات الوزيرة ليان عن تقارب بين زعماء المناطق.");
            }
            else if (eventIndex == 1)
            {
                var advisor = GetAdvisor("marshal");
                advisor.Loyalty = Mathf.Clamp(advisor.Loyalty + 3, 0, 100);
                AddEvent("أشاد الحرس بتدريبات القائد راشد، فازدادت ثقته بالمجلس.");
            }
            else
            {
                var advisor = GetAdvisor("steward");
                advisor.Influence = Mathf.Clamp(advisor.Influence + 2, 0, 100);
                AddEvent("حسّنت أمينة الخزانة مريم سجلات المخازن.");
            }
        }

        private void AddEvent(string message)
        {
            EventLog.Insert(0, message);
            if (EventLog.Count > MaxLogEntries)
                EventLog.RemoveAt(EventLog.Count - 1);
        }
    }
}
