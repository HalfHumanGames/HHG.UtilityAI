using System.Collections.Generic;
using UnityEngine;

namespace HHG.UtilityAI.Runtime
{
    public class SoftmaxSelector<TContext> : ITaskSelector<TContext>
    {
        private readonly float temperature;

        public SoftmaxSelector(float temperature = 1f)
        {
            this.temperature = Mathf.Clamp(temperature, .01f, 1f);
        }

        public Task<TContext> Select(Dictionary<Task<TContext>, float> scoredTasks)
        {
            float sum = 0f;
            Task<TContext> best = null;
            float bestScore = float.NegativeInfinity;

            foreach (var kv in scoredTasks)
            {
                float expScore = Mathf.Exp(kv.Value / temperature);
                sum += expScore;

                if (expScore > bestScore)
                {
                    bestScore = expScore;
                    best = kv.Key;
                }
            }

            float roll = Random.Range(0f, sum);
            float cumulative = 0f;

            foreach (var kv in scoredTasks)
            {
                float expScore = Mathf.Exp(kv.Value / temperature);
                cumulative += expScore;

                if (roll <= cumulative) return kv.Key;
            }

            return best;
        }
    }
}
