using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace StudyXR.LakeFishing
{
    public class CatchLog : MonoBehaviour
    {
        [Header("연결")]
        [SerializeField] private TMP_Text panelText;

        [Header("표시")]
        [SerializeField] private string title = "오늘의 조과";
        [SerializeField] private int maxRows = 5;

        private readonly List<string> rows = new List<string>();
        private readonly StringBuilder builder = new StringBuilder();
        private int caught;
        private int missed;
        private float bestLength;
        private string bestSpecies = "";

        private int keptCount;
        public float BestLength => bestLength;

        private void Start()
        {
            Refresh();
        }

        public void Add(
            string species,
            float lengthMeters,
            bool kept = true)
        {
            caught++;
            if (kept) keptCount++;
            if (lengthMeters > bestLength)
            {
                bestLength = lengthMeters;
                bestSpecies = species;
            }

            rows.Insert(0, $"{System.DateTime.Now:HH:mm}  {species}  {lengthMeters * 100f:F0} cm  {CatchDisplay.WeightKg(lengthMeters):F2} kg  {(kept ? "보관" : "놓아줌")}");
            while (rows.Count > maxRows) rows.RemoveAt(rows.Count - 1);
            Refresh();
        }

        public void AddMiss()
        {
            missed++;
            Refresh();
        }

        private void Refresh()
        {
            if (panelText == null) return;

            builder.Clear();
            builder.Append(title).Append('\n');
            if (caught > 0)
                builder.Append($"잡음 {caught} (보관 {keptCount}) · 놓침 {missed} · 최고 {bestSpecies} {bestLength * 100f:F0} cm");
            else builder.Append($"잡음 0 · 놓침 {missed}");
            for (int i = 0; i < rows.Count; ++i) builder.Append('\n').Append(rows[i]);
            panelText.text = builder.ToString();
        }
    }
}
