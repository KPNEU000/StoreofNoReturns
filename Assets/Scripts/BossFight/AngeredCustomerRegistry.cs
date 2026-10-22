using System.Collections.Generic;
using BossFight.Rules;

namespace BossFight
{
    // Which customers walked away angry; its Count is the monster count of the boss fight.
    public static class AngeredCustomerRegistry
    {
        private static readonly Dictionary<string, bool> angered = new Dictionary<string, bool>();

        public static int Count
        {
            get
            {
                int count = 0;
                foreach (var entry in angered)
                {
                    if (entry.Value) count++;
                }
                return count;
            }
        }

        public static IEnumerable<string> Names
        {
            get
            {
                foreach (var entry in angered)
                {
                    if (entry.Value) yield return entry.Key;
                }
            }
        }

        // Records the outcome of one interaction; the latest result for a name wins.
        public static bool Record(string customerName, int reputationAtStart, int reputationAtEnd)
        {
            bool isAngered = AngerRule.IsAngered(reputationAtStart, reputationAtEnd);
            angered[customerName] = isAngered;
            return isAngered;
        }

        public static bool IsAngered(string customerName)
        {
            return angered.TryGetValue(customerName, out bool value) && value;
        }

        public static void Reset()
        {
            angered.Clear();
        }
    }
}
