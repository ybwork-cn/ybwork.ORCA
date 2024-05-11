using Unity.Collections;
using Unity.Jobs;
using static Nebukam.JobAssist.Extensions;

namespace Nebukam.JobAssist
{

    public interface IProcessorGroup : IProcessorCompound
    {

    }

    /// <summary>
    /// A ProcessorGroup starts its child processors at the same time 
    /// and return a combined handle
    /// </summary>
    public abstract class ProcessorGroup : AbstractProcessorCompound, IProcessorGroup
    {

        protected NativeArray<JobHandle> _groupHandles = default;

        #region Scheduling

        internal override void OnPrepare()
        {
            MakeLength(ref _groupHandles, _enabledChildren);
            base.OnPrepare();
        }

        internal override JobHandle OnScheduled(IProcessor dependsOn = null)
        {

            if (_isCompoundEmpty) { return ScheduleEmpty(dependsOn); }

            int count = Count;
            IProcessor proc;
            IProcessorCompound comp;

            for (int i = 0; i < count; i++)
            {
                proc = _childs[i];
                comp = proc as IProcessorCompound;
                if (!proc.enabled
                    || (comp != null && comp.isCompoundEmpty))
                { continue; } // Skip disabled and/or empty

                _groupHandles[i] = proc.Schedule(_scaledLockedDelta, dependsOn);
            }

            return JobHandle.CombineDependencies(_groupHandles);

        }

        internal override JobHandle OnScheduled(JobHandle dependsOn)
        {

            if (_isCompoundEmpty) { return ScheduleEmpty(dependsOn); }

            int count = Count;
            IProcessor proc;
            IProcessorCompound comp;

            for (int i = 0; i < count; i++)
            {
                proc = _childs[i];
                comp = proc as IProcessorCompound;
                if (!proc.enabled
                    || (comp != null && comp.isCompoundEmpty))
                { continue; } // Skip disabled and/or empty

                _groupHandles[i] = proc.Schedule(_scaledLockedDelta, dependsOn);
            }

            return JobHandle.CombineDependencies(_groupHandles);

        }

        #endregion

        #region Complete & Apply

        protected sealed override void OnCompleteEnds() { }

        #endregion

        #region IDisposable

        protected override void InternalDispose()
        {
            _groupHandles.Release();
        }

        #endregion

        #region Abstracts
        /*
        protected override void InternalLock() { }

        protected override void Prepare(float delta) { }

        protected override void Apply() { }

        protected override void InternalUnlock() { }
        */
        #endregion

    }
}
