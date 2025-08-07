namespace HHG.UtilityAI.Runtime
{
    public interface IConsideration<TContext>
    {
        public float Weight => 1f;
        public bool TryScore(Task<TContext> task, TContext context, out float score);
    }
}