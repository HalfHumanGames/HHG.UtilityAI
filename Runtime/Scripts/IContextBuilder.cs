using System.Collections;

namespace HHG.UtilityAI.Runtime
{
    public interface IContextBuilder<TContext>
    {
        public IEnumerator BuildContextAsync(TContext context);
    }
}