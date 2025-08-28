using HHG.Common.Runtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HHG.UtilityAI.Runtime
{
    public class Agent<TContext> where TContext : class, new()
    {
        private readonly IContextBuilder<TContext> contextBuilder;
        private readonly ITaskBuilder<TContext> taskBuilder;
        private readonly ITaskSelector<TContext> taskSelector;
        private readonly Dictionary<Task<TContext>, float> scoredTasks = new();
        private readonly Stack<IEnumerator> executionStack = new();
        private readonly int sliceSize = 100;
        private readonly TContext context = new();
        private readonly List<Task<TContext>> tasks = new();

        public Agent(
            IContextBuilder<TContext> contextBuilder,
            ITaskBuilder<TContext> taskBuilder,
            ITaskSelector<TContext> taskSelector = null,
            int sliceSize = -1)
        {
            this.contextBuilder = contextBuilder;
            this.taskBuilder = taskBuilder;
            this.taskSelector = taskSelector ?? new GreedySelector<TContext>();
            this.sliceSize = sliceSize;
        }

        public IEnumerator Execute()
        {
            while (true)
            {
                scoredTasks.Clear();

                // Builders are async in case need to get
                // data over several frames for any reason
                yield return contextBuilder.BuildContextAsync(context);

                // Context building may be processor intensive
                // so wait a frame before building tasks
                yield return new WaitForEndOfFrame();

                // Builders are async in case need to get
                // data over several frames for any reason
                yield return taskBuilder.BuildTasksAsync(context, tasks);

                // Building tasks may also be processor intensive
                // so wait a frame before scoring tasks
                yield return new WaitForEndOfFrame();

                // Filter out invalid tasks that break any rules
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
