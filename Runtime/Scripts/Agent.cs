using HHG.Common.Runtime;
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
        private readonly int sliceSize = 100;
        private TContext context;

        public Agent(
            IContextProvider<TContext> contextProvider,
            ITaskBuilder<TContext> taskBuilder,
            ITaskSelector<TContext> taskSelector = null,
            int sliceSize = -1)
        {
            this.contextProvider = contextProvider;
            this.taskBuilder = taskBuilder;
            this.taskSelector = taskSelector ?? new GreedySelector<TContext>();
            this.sliceSize = sliceSize;
        }

        public IEnumerator Execute()
        {
            while (true)
            {
                scoredTasks.Clear();

                context = contextProvider.GetContext();

                // Context building may be processor intensive
                // so wait a frame before building tasks
                yield return new WaitForEndOfFrame();

                var tasks = taskBuilder.BuildTasks(context);

                // Building tasks may also be processor intensive
                // so wait a frame before scoring tasks
                yield return new WaitForEndOfFrame();

                var validTasks = tasks.Where(t => t.Rules.All(r => r.IsValid(t, context)));

                // Scoring tasks may also be processor intensive
                // So YieldSliced to score over several frames
                yield return CoroutineUtil.YieldSliced(validTasks, sliceSize, ComputeScore);

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

        private void ComputeScore(Task<TContext> task)
        {
            float totalScore = 0f;
            float totalWeight = 0f;

            bool valid = true;
            foreach (var consideration in task.Considerations)
            {
                if (consideration.TryScore(task, context, out float score))
                {
                    totalScore += score * consideration.Weight;
                    totalWeight += consideration.Weight;
                }
                else
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                float finalScore = totalScore / totalWeight;
                scoredTasks[task] = finalScore;
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
