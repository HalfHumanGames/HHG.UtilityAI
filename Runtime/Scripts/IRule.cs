namespace HHG.UtilityAI.Runtime
{
    public interface IRule<TContext>
    {
        public bool IsValid(Task<TContext> task, TContext context);
    }
}