namespace HHG.UtilityAI.Runtime
{
    public interface IContextProvider<TContext>
    {
        public TContext GetContext();
    }
}