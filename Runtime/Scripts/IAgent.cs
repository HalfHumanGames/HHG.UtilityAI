using System.Collections;

namespace HHG.UtilityAI.Runtime
{
    public interface IAgent
    {
        bool IsPaused { get; }

        void Pause();
        void Resume();
        void Cancel();
        void Replan();

        IEnumerator Execute();
    }
}