using Unity.Jobs;

namespace Nebukam.JobAssist
{
    public abstract class Processor<T> : AbstractProcessor, IProcessor
        where T : struct, IJob
    {
        protected T _currentJob;

        #region Scheduling

        internal override void OnPrepare()
        {
            Prepare(ref _currentJob, _scaledLockedDelta);
        }

        internal override JobHandle OnScheduled(IProcessor dependsOn = null)
        {
            return dependsOn == null
                ? _currentJob.Schedule()
                : _currentJob.Schedule(dependsOn.currentHandle);
        }

        internal override JobHandle OnScheduled(JobHandle dependsOn)
        {
            return _currentJob.Schedule(dependsOn);
        }

        protected abstract void Prepare(ref T job, float delta);

        #endregion

        #region Complete & Apply

        protected sealed override void OnCompleteBegins()
        {
            _currentHandle.Complete();
        }

        protected sealed override void OnCompleteEnds()
        {
            Apply(ref _currentJob);
        }

        protected virtual void Apply(ref T job) { }

        #endregion

        #region ILockable

        public sealed override void Lock()
        {
            if (_locked) { return; }
            _currentJob = default;
            base.Lock();
        }

        public sealed override void Unlock()
        {
            base.Unlock();
        }

        #endregion
    }
}
