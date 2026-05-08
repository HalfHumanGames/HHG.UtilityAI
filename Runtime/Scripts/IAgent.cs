using System.Collections;

namespace HHG.UtilityAI.Runtime
{
    public interface IAgent
    {
        void RequestCancel();
        void RequestReplan();
        IEnumerator Execute();
    }
}