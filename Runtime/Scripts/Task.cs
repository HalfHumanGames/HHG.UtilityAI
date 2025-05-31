using System.Collections;
using System.Collections.Generic;

namespace HHG.UtilityAI.Runtime
{
    public abstract class Task<TContext>
    {
        public IReadOnlyList<IRule<TContext>> Rules => rules;
        public IReadOnlyList<IConsideration<TContext>> Considerations => considerations;

        protected List<IRule<TContext>> rules = new List<IRule<TContext>>();
        protected List<IConsideration<TContext>> considerations = new List<IConsideration<TContext>>();

        public abstract IEnumerator Execute(TContext context);
    }
}