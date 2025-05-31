using System.Collections.Generic;

namespace HHG.UtilityAI.Runtime
{
    public interface ITaskBuilder<TContext>
    {
        public IReadOnlyList<Task<TContext>> BuildTasks(TContext context);
    }
}