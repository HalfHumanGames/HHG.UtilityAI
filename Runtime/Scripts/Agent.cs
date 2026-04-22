using HHG.Common.Runtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HHG.UtilityAI.Runtime
{
    public class Agent<TContext> : IAgent where TContext : class, new()
    {
        private readonly IContextBuilder<TContext> contextBuilder;
        private readonly ITaskBuilder<TContext> taskBuilder;
        private readonly ITaskSelector<TContext> taskSelector;
        private readonly List<Task<TContext>> tasks = new();
        private readonly Dictionary<Task<TContext>, float> scoredTasks = new();
        private readonly Stack<IEnumerator> executionStack = new();
        private readonly int sliceSize = 100;
        private readonly TContext context = new();

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
                tasks.Clear();
                scoredTasks.Clear();

                // Builders are async in case need to get
                // data over several frames for any reason
                yield return contextBuilder.BuildContextAsync(context);

                // Context building may be processor intensive
                // so wait a frame before building tasks
                yield return null;

                // Builders are async in case need to get
                // data over several frames for any reason
                yield return taskBuilder.BuildTasksAsync(context, tasks);

                // Building tasks may also be processor intensive
                // so wait a frame before scoring tasks
                yield return null;

                // Filter out invalid tasks that break any rules
                var validTasks = tasks.Where(t => t.Rules.All(r => r.IsValid(t, context)));

                // Scoring tasks may also be processor intensive
                // So use StartCoroutineSliced to do over several frames
                yield return CoroutineUtil.StartCoroutineSliced(validTasks, sliceSize, ComputeScore);

                bool cancel = false;
                bool replan = false;
                var selected = taskSelector.Select(scoredTasks);

                if (selected != null)
                {
                    var execution = Flatten(selected.Execute(context));

                    while (execution.MoveNext())
                    {
                        object current = execution.Current;
                        cancel = current is CancelRequest;
                        replan = current is ReplanRequest;

                        // Do not yield break! Must break the loop
                        // so code can continue to allow builders
                        // to dispose of the context and task list
                        if (cancel || replan) break;

                        yield return current;
                    }
                }

                // Optional builder cleanup
                contextBuilder.Dispose(context);
                taskBuilder.Dispose(tasks);

                // Exit if done or cancelled
                if (!replan) break;
            }
        }

        // Compute score as a weighted average
        private void ComputeScore(Task<TContext> task)
        {
            float totalScore = 0f;
            float totalWeight = 0f;

            foreach (var consideration in task.Considerations)
            {
                if (!consideration.TryScore(task, context, out float score))
                {
                    return;
                }

                totalScore += score * consideration.Weight;
                totalWeight += consideration.Weight;
            }

            float finalScore = totalWeight == 0f ? 0f : totalScore / totalWeight * task.Weight;
            scoredTasks[task] = finalScore;
        }

        // Need to flatten the exection enumerator in order
        // to catch and handle replan requests properly.
        private IEnumerator Flatten(IEnumerator enumerator)
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
    }

    public class CancelRequest
    {
        public static readonly CancelRequest Instance = new CancelRequest();
    }
}
