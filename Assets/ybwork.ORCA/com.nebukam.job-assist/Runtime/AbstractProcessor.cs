using System;
using Unity.Jobs;

namespace Nebukam.JobAssist
{
    public interface IProcessor : IDisposable, ILockable
    {
        /// <summary>
        /// Whether this processor is enabled or not.
        /// Note that this property is only accounted for by compounds.
        /// While disabled a Processor can still be found by TryGetFirst, TryGetFirstInCompount & Find
        /// </summary>
        bool enabled { get; set; }

        /// <summary>
        /// User-defined delta multiplier.
        /// </summary>
        float deltaMultiplier { get; set; }

        /// <summary>
        /// Parent compound for this processor, if any
        /// </summary>
        IProcessorCompound compound { get; set; }
        /// <summary>
        /// Index of this processor inside its parent
        /// </summary>
        int compoundIndex { get; set; }

        /// <summary>
        /// Return whether or not this processor' job is scheduled
        /// </summary>
        bool scheduled { get; }
        /// <summary>
        /// Return whether or not this processor' job is completed
        /// </summary>
        bool completed { get; }

        /// <summary>
        /// Return the current IProcessor dependency, if any.
        /// </summary>
        IProcessor procDependency { get; }
        /// <summary>
        /// Return the current JobHandle dependency, if any.
        /// </summary>
        JobHandle currentHandle { get; }

        /// <summary>
        /// Schedule the processor' job if not scheduled already.
        /// </summary>
        /// <param name="delta"></param>
        /// <param name="dependsOn">IProcessor dependency.</param>
        /// <returns></returns>
        JobHandle Schedule(float delta, IProcessor dependsOn = null);
        /// <summary>
        /// Schedule the processor' job if not scheduled already.
        /// </summary>
        /// <param name="delta"></param>
        /// <param name="dependsOn">JobHandle dependency.</param>
        /// <returns></returns>
        JobHandle Schedule(float delta, JobHandle dependsOn);

        /// <summary>
        /// Completes the job.
        /// </summary>
        void Complete();

        /// <summary>
        /// Completes the job only if it is finished.
        /// Return false if the job hasn't been scheduled.
        /// </summary>
        /// <returns>Whether the job has been completed or not</returns>
        bool TryComplete();

        /// <summary>
        /// Schedules and immediately completes the job
        /// </summary>
        /// <param name="delta"></param>
        void Run(float delta = 0f);
    }

    public abstract class AbstractProcessor : IProcessor
    {
        public float deltaMultiplier { get; set; } = 1.0f;

        protected bool _locked = false;
        public bool locked { get { return _locked; } }

        protected IProcessorCompound _compound = null;
        public IProcessorCompound compound { get { return _compound; } set { _compound = value; } }

        public int compoundIndex { get; set; } = -1;

        protected bool _hasJobHandleDependency = false;
        protected JobHandle _jobHandleDependency = default;

        protected IProcessor _procDependency = null;
        public IProcessor procDependency { get { return _procDependency; } }

        protected JobHandle _currentHandle;
        public JobHandle currentHandle { get { return _currentHandle; } }

        protected float _lockedDelta = 0f;
        protected float _scaledLockedDelta = 0f;
        protected float _deltaSum = 0f;

        protected bool _scheduled = false;
        public bool scheduled { get { return _scheduled; } }
        public bool completed { get { return _scheduled && _currentHandle.IsCompleted; } }

        protected bool _enabled = true;
        public bool enabled
        {
            get { return _enabled; }
            set
            {
                if (_locked)
                {
                    throw new Exception("You cannot change a processor status while it is locked.");
                }

                _enabled = value;
            }
        }

#if UNITY_EDITOR
        protected bool _disposed = false;
#endif

        #region Scheduling

        public JobHandle Schedule(float delta, IProcessor dependsOn = null)
        {
#if UNITY_EDITOR
            if (_disposed)
            {
                throw new Exception("Schedule() called on disposed Processor ( " + GetType().Name + " ).");
            }
#endif
            _deltaSum += delta;

            if (_scheduled) { return _currentHandle; }

            _scheduled = true;
            _hasJobHandleDependency = false;
            _procDependency = dependsOn;

            Lock();

            OnPrepare();

            _currentHandle = OnScheduled(_procDependency);

            return _currentHandle;
        }

        public JobHandle Schedule(float delta, JobHandle dependsOn)
        {
#if UNITY_EDITOR
            if (_disposed)
            {
                throw new Exception("Schedule() called on disposed Processor ( " + GetType().Name + " ).");
            }
#endif
            _deltaSum += delta;

            if (_scheduled) { return _currentHandle; }

            _scheduled = true;
            _hasJobHandleDependency = true;
            _jobHandleDependency = dependsOn;
            _procDependency = null;

            Lock();

            OnPrepare();

            _currentHandle = OnScheduled(dependsOn);

            return _currentHandle;
        }

        internal abstract void OnPrepare();

        internal abstract JobHandle OnScheduled(IProcessor dependsOn = null);

        internal abstract JobHandle OnScheduled(JobHandle dependsOn);

        #endregion

        #region Complete & Apply

        /// <summary>
        /// Complete the job.
        /// </summary>
        public void Complete()
        {
#if UNITY_EDITOR
            if (_disposed)
            {
                throw new Exception("Complete() called on disposed Processor ( " + GetType().Name + " ).");
            }
#endif

            if (!_scheduled) { return; }

            // Complete dependencies

            if (_hasJobHandleDependency)
                _jobHandleDependency.Complete();

            _procDependency?.Complete();

            // Complete self

            OnCompleteBegins();

            _scheduled = false;

            OnCompleteEnds();

            Unlock();
        }

        protected abstract void OnCompleteBegins();

        protected abstract void OnCompleteEnds();

        public bool TryComplete()
        {
            if (!_scheduled) { return false; }
            if (completed)
            {
                Complete();
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Run(float delta = 0f)
        {
            Schedule(delta);
            Complete();
        }

        #endregion

        #region ILockable

        public virtual void Lock()
        {
            if (_locked) { return; }
            _lockedDelta = _deltaSum;
            _scaledLockedDelta = _lockedDelta * deltaMultiplier;
            _deltaSum = 0f;
            InternalLock();
            _locked = true;
        }

        protected virtual void InternalLock() { }

        public virtual void Unlock()
        {
            if (!_locked) { return; }
            _locked = false;
            if (_scheduled) { Complete(); } //Complete the job for safety
            InternalUnlock();
        }

        protected virtual void InternalUnlock() { }

        #endregion

        #region Hierarchy

        protected bool TryGetFirstInCompound<P>(out P processor, bool deep = false)
            where P : class, IProcessor
        {
            processor = null;
            if (_compound != null && compoundIndex >= 0)
            {
                // If compoundIndex == 0, need to go upward in compounds
                return _compound.TryGetFirst(compoundIndex - 1, out processor, deep);
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region IDisposable

        protected void Dispose(bool disposing)
        {

            if (!disposing) { return; }
#if UNITY_EDITOR
            _disposed = true;
#endif

            //Complete the job first so we can rid of unmanaged resources.
            if (_scheduled) { _currentHandle.Complete(); }

            InternalDispose();

            _procDependency = null;
            _scheduled = false;

        }

        protected virtual void InternalDispose() { }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
