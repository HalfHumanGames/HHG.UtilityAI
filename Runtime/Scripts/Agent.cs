using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HHG.UtilityAI.Runtime
{
    public class Agent<TContext>
    {
        private readonly IContextProvider<TContext> contextProvider;
        private readonly ITaskBuilder<TContext> taskBuilder;
        private readonly ITaskSelector<TContext> taskSelector;
        private readonly Dictionary<Task<TContext>, float> scoredTasks = new Dictionary<Task<TContext>, float>();
        private readonly Stack<IEnumerator> executionStack = new Stack<IEnumerator>();

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
            while (true)
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
                
                if (selected == null) yield break;
                
                var execution = Flatten(selected.Execute(context));

                while (execution.MoveNext())
                {
                    if (execution.Current is ReplanRequest) break;

                    yield return execution.Current;
                }

                if (execution.Current is not ReplanRequest) break;
            }
        }

        // Need to flatten the exection enumerator in order
        // to catch and handle replan requests properly.
        public IEnumerator Flatten(IEnumerator enumerator)
        {
            executionStack.Clear();
            executionStack.Push(enumerator);

            while (executionStack.Count > 0)
            {
                var currentEnumerator = executionStack.Peek();

                if (!currentEnumerator.MoveNext())
                {
                    executionStack.Pop();
                    continue;
                }

                var current = currentEnumerator.Current;

                if (current is IEnumerator nestedEnumerator)
                {
                    executionStack.Push(nestedEnumerator);
                }
                else
                {
                    yield return current;
                }
            }
        }
    }

    public class ReplanRequest
    {
        public static readonly ReplanRequest Instance = new ReplanRequest();

        private ReplanRequest() { }
    }
}
