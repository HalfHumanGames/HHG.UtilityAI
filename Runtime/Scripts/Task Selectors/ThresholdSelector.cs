using System.Collections.Generic;
using System.Linq;

namespace HHG.UtilityAI.Runtime
{
    public class ThresholdSelector<TContext> : ITaskSelector<TContext>
    {
        private readonly float threshold;

        public ThresholdSelector(float threshold = 0.5f)
        {
            this.threshold = threshold;
        }

        public Task<TContext> Select(Dictionary<Task<TContext>, float> scoredTasks)
        {
            return scoredTasks.Where(kv => kv.Value >= threshold).OrderByDescending(kv => kv.Value).FirstOrDefault().Key;
        }
    }
}