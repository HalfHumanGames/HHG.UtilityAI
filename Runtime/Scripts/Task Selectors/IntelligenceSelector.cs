using HHG.Common.Runtime;
using System.Collections.Generic;
using UnityEngine;

namespace HHG.UtilityAI.Runtime
{
    public class IntelligenceSelector<TContext> : ITaskSelector<TContext>
    {
        private readonly float intelligence;
        private readonly float epsilon;

        public IntelligenceSelector(float intelligence = 1f, float epsilon = .01f)
        {
            this.intelligence = Mathf.Clamp01(intelligence);
            this.epsilon = Mathf.Max(0f, epsilon);
        }

        public Task<TContext> Select(Dictionary<Task<TContext>, float> scoredTasks)
        {
            float minScore = float.MaxValue;
            float maxScore = float.MinValue;

            foreach (var kvpair in scoredTasks)
            {
                if (kvpair.Value < minScore) minScore = kvpair.Value;
                if (kvpair.Value > maxScore) maxScore = kvpair.Value;
            }

            float targetScore = Mathf.Lerp(minScore, maxScore, intelligence);

            using (Pool.GetList(out List<Task<TContext>> candidates))
            {
                float closestDistance = float.MaxValue;
                Task<TContext> fallbackTask = null;

                foreach (var kvpair in scoredTasks)
                {
                    float distance = Mathf.Abs(kvpair.Value - targetScore);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        fallbackTask = kvpair.Key;
                    }
                    if (distance <= epsilon)
                    {
                        candidates.Add(kvpair.Key);
                    }
                }

                if (candidates.Count == 0) return fallbackTask;

                return candidates.GetRandom();
            }
        }
    }
}
