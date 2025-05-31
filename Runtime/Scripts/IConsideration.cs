namespace HHG.UtilityAI.Runtime
{
    public interface IConsideration<TContext>
    {
        public float Weight => 1f;
        public float Score(Task<TContext> task, TContext context);
    }
}