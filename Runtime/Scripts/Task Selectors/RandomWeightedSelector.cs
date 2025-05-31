using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HHG.UtilityAI.Runtime
{
    public class RandomWeightedSelector<TContext> : ITaskSelector<TContext>
    {
        public Task<TContext> Select(Dictionary<Task<TContext>, float> scoredTasks)
        {
            float total = scoredTasks.Sum(kv => kv.Value);

            if (total <= 0f) return null;

            float roll = Random.Range(0f, 1f) * total;
            float cumulative = 0f;

            foreach (var kv in scoredTasks.OrderByDescending(k => k.Value))
            {
                cumulative += kv.Value;

                if (roll <= cumulative) return kv.Key;
            }

            return scoredTasks.OrderByDescending(k => k.Value).FirstOrDefault().Key;
        }
    }
}