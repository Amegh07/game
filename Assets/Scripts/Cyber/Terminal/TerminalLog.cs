using System.Collections.Generic;
using UnityEngine;

namespace MuseumHeist.Cyber
{
    public class TerminalLog : MonoBehaviour
    {
        [SerializeField] private int maxEntries = 100;

        private readonly List<TerminalLogEntry> entries = new();
        private readonly List<TerminalLogEntry> readableEntries = new();

        public event System.Action<TerminalLogEntry> OnEntryAdded;

        public void AddEntry(TerminalLogEntry entry)
        {
            entries.Add(entry);
            readableEntries.Add(entry);
            OnEntryAdded?.Invoke(entry);

            while (entries.Count > maxEntries)
                entries.RemoveAt(0);
        }

        public void AddEntry(string userName, UserRole role, string action, string result, bool success)
        {
            AddEntry(new TerminalLogEntry(userName, role, action, result, success));
        }

        public IReadOnlyList<TerminalLogEntry> GetEntries()
        {
            return entries;
        }

        public int Count => entries.Count;

        public void Clear()
        {
            entries.Clear();
            readableEntries.Clear();
        }
    }
}
