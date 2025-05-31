using HHG.Common.Runtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HHG.UtilityAI.Runtime
{
    public class TopNRandomSelector<TContext> : ITaskSelector<TContext>
    {
        private readonly int topN;

        public TopNRandomSelector(int topN = 3)
        {
            this.topN = Mathf.Max(topN, 1);
        }

        public Task<TContext> Select(Dictionary<Task<TContext>, float> scoredTasks)
        {
            return scoredTasks.OrderByDescending(kv => kv.Value).Take(topN).Shuffled().FirstOrDefault().Key;
        }
    }
}