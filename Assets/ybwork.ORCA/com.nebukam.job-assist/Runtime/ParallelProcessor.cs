using Unity.Jobs;

namespace Nebukam.JobAssist
{
    public interface IParallelProcessor : IProcessor
    {
        int chunkSize { get; set; }
    }

    public abstract class ParallelProcessor<T> : AbstractProcessor, IParallelProcessor
        where T : struct, IJobParallelFor
    {
        protected T _currentJob;
        public int chunkSize { get; set; } = 64;

        protected int _jobLength = 0;

        #region Scheduling

        internal override void OnPrepare()
        {
            _jobLength = Prepare(ref _currentJob, _scaledLockedDelta);
        }

        internal override JobHandle OnScheduled(IProcessor dependsOn = null)
        {
            return dependsOn == null
                ? _currentJob.Schedule(_jobLength, chunkSize)
                : _currentJob.Schedule(_jobLength, chunkSize, dependsOn.currentHandle);
        }

        internal override JobHandle OnScheduled(JobHandle dependsOn)
        {
            return _currentJob.Schedule(_jobLength, chunkSize, dependsOn);
        }

        protected abstract int Prepare(ref T job, float delta);

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
