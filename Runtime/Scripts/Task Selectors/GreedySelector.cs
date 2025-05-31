using System.Collections.Generic;
using System.Linq;

namespace HHG.UtilityAI.Runtime
{
    public class GreedySelector<TContext> : ITaskSelector<TContext>
    {
        public Task<TContext> Select(Dictionary<Task<TContext>, float> scoredTasks)
        {
            return scoredTasks.OrderByDescending(kv => kv.Value).FirstOrDefault().Key;
        }
    }
}