using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HHG.UtilityAI.Runtime
{
    public class Agent<TContext>
    {
        private readonly IContextProvider<TContext> contextProvider;
        private readonly ITaskBuilder<TContext> taskBuilder;
        private readonly ITaskSelector<TContext> taskSelector;
        private readonly Dictionary<Task<TContext>, float> scoredTasks = new Dictionary<Task<TContext>, float>();

        public Agent(
            IContextProvider<TContext> contextProvider,
            ITaskBuilder<TContext> taskBuilder, 
            ITaskSelector<TContext> taskSelector = null)
        {
            this.contextProvider = contextProvider;
            this.taskBuilder = taskBuilder;
            this.taskSelector = taskSelector ?? new GreedySelector<TContext>();
        }

        public IEnumerator Execute()
        {
            scoredTasks.Clear();

            var context = contextProvider.GetContext();
            var tasks = taskBuilder.BuildTasks(context);
            var validTasks = tasks.Where(t => t.Rules.All(r => r.IsValid(t, context)));

            foreach (var task in validTasks)
            {
                float totalScore = 0f;
                float totalWeight = 0f;

                foreach (var consideration in task.Considerations)
                {
                    float score = consideration.Score(task, context);
                    totalScore += score * consideration.Weight;
                    totalWeight += consideration.Weight;
                }

                if (totalWeight <= 0f) continue;

                float finalScore = totalScore / totalWeight;
                scoredTasks[task] = finalScore;
            }

            var selected = taskSelector.Select(scoredTasks);
            yield return selected?.Execute(context);
        }
    }
}
