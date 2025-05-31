using System.Collections;
using System.Collections.Generic;

namespace HHG.UtilityAI.Runtime
{
    public class TaskComposite<TContext> : Task<TContext>
    {
        public List<Task<TContext>> Tasks = new();

        public override IEnumerator Execute(TContext context)
        {
            foreach (Task<TContext> action in Tasks)
            {
                yield return action.Execute(context);
            }
        }

        public override string ToString()
        {
            return string.Join(", ", Tasks);
        }
    }
}