using HHG.Common.Runtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HHG.UtilityAI.Runtime
{
    public class EpsilonGreedySelector<TContext> : ITaskSelector<TContext>
    {
        private readonly float epsilon;

        public EpsilonGreedySelector(float epsilon = 0.2f)
        {
            this.epsilon = epsilon;
        }

        public Task<TContext> Select(Dictionary<Task<TContext>, float> scoredTasks)
        {
            if (Random.Range(0f, 1f) < epsilon)
            {
                return scoredTasks.Keys.RandomOrDefault();
            }

            return scoredTasks.OrderByDescending(kv => kv.Value).FirstOrDefault().Key;
        }
    }

}