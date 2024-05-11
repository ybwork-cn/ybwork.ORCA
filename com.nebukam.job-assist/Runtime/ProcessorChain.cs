using Unity.Jobs;

namespace Nebukam.JobAssist
{
    public interface IProcessorChain : IProcessorCompound
    {
    }

    /// <summary>
    /// A ProcessorChain chains its child processor and return the last
    /// job of the chain as its handle.
    /// </summary>
    public abstract class ProcessorChain : AbstractProcessorCompound, IProcessorChain
    {
        #region Scheduling

        internal override JobHandle OnScheduled(IProcessor dependsOn = null)
        {

            if (_isCompoundEmpty) { return ScheduleEmpty(dependsOn); }

            int count = _childs.Count;
            IProcessor proc, prevProc = dependsOn;
            IProcessorCompound comp;
            JobHandle handle = default;

            for (int i = 0; i < count; i++)
            {
                proc = _childs[i];
                proc.compoundIndex = i; // Redundant ?
                comp = proc as IProcessorCompound;

                if (!proc.enabled
                    || (comp != null && comp.isCompoundEmpty))
                { continue; } // Skip disabled and/or empty



                handle = prevProc == null
                    ? proc.Schedule(_scaledLockedDelta)
                    : proc.Schedule(_scaledLockedDelta, prevProc); ;
                prevProc = proc;

            }

            return handle;

        }

        internal override JobHandle OnScheduled(JobHandle dependsOn)
        {

            if (_isCompoundEmpty) { return ScheduleEmpty(dependsOn); }

            int count = _childs.Count;
            IProcessor proc, prevProc = null;
            IProcessorCompound comp;
            JobHandle handle = default;

            for (int i = 0; i < count; i++)
            {
                proc = _childs[i];
                proc.compoundIndex = i; // Redundant ?

                comp = proc as IProcessorCompound;

                if (!proc.enabled
                    || (comp != null && comp.isCompoundEmpty))
                { continue; } // Skip disabled and/or empty

                handle = prevProc == null
                    ? proc.Schedule(_scaledLockedDelta, _jobHandleDependency)
                    : proc.Schedule(_scaledLockedDelta, prevProc);
                prevProc = proc;

            }

            return handle;
        }

        #endregion

        protected sealed override void OnCompleteEnds() { }
    }
}
