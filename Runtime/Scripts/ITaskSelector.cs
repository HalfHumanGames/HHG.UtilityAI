using System.Collections.Generic;

namespace HHG.UtilityAI.Runtime
{
    public interface ITaskSelector<TContext>
    {
        public Task<TContext> Select(Dictionary<Task<TContext>, float> scoredTasks);
    }
}