using System.Collections;
using System.Collections.Generic;

namespace HHG.UtilityAI.Runtime
{
    public interface ITaskBuilder<TContext>
    {
        public IEnumerator BuildTasksAsync(TContext context, List<Task<TContext>> tasks);
        public void Dispose(List<Task<TContext>> tasks) { }
    }
}