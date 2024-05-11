using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace Nebukam.JobAssist
{
    public interface IProcessorCompound : IProcessor
    {
        /// <summary>
        /// Return the current number of children in this compound
        /// </summary>
        int Count { get; }

        bool isCompoundEmpty { get; }

        /// <summary>
        /// Return the child stored at a given index
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        IProcessor this[int i] { get; }

        /// <summary>
        /// Dispose of the compound as well as all of its childrens. 
        /// Recursive.
        /// </summary>
        void DisposeAll();

        /// <summary>
        /// Attempt to find the first item of type P
        /// </summary>
        /// <typeparam name="P"></typeparam>
        /// <param name="startIndex"></param>
        /// <param name="processor"></param>
        /// <param name="deep"></param>
        /// <returns></returns>
        bool TryGetFirst<P>(int startIndex, out P processor, bool deep = false) where P : class, IProcessor;
        bool Find<P>(out P processor) where P : class, IProcessor;
    }

    public abstract class AbstractProcessorCompound : AbstractProcessor, IProcessorCompound
    {
        protected EmptyCompound _emptyCompoundJob;
        protected bool _isCompoundEmpty = false;
        public bool isCompoundEmpty { get { return _isCompoundEmpty; } }

        protected int _enabledChildren = 0;

        protected List<IProcessor> _childs = new List<IProcessor>();
        public int Count { get { return _childs.Count; } }

        public IProcessor this[int i] { get { return _childs[i]; } }

        #region Child management

        public IProcessor Add(IProcessor proc)
        {
#if UNITY_EDITOR
            if (_locked)
            {
                throw new Exception("You cannot add new processors to a locked chain");
            }
#endif

            if (_childs.Contains(proc)) { return proc; }
            _childs.Add(proc);
            return OnChildAdded(proc, Count - 1);
        }

        public P Add<P>()
            where P : class, IProcessor, new()
        {
            P proc = null;
            return Add(ref proc);
        }

        public P Add<P>(P proc)
            where P : class, IProcessor
        {
#if UNITY_EDITOR
            if (_locked)
            {
                throw new Exception("You cannot add new processors to a locked chain");
            }
#endif

            if (_childs.Contains(proc)) { return proc; }
            _childs.Add(proc);
            return OnChildAdded(proc, Count - 1) as P;
        }

        /// <summary>
        /// Create (if null) and add item
        /// </summary>
        /// <typeparam name="P"></typeparam>
        /// <param name="proc"></param>
        /// <returns></returns>
        public P Add<P>(ref P proc)
            where P : class, IProcessor, new()
        {
#if UNITY_EDITOR
            if (_locked)
            {
                throw new Exception("You cannot add new processors to a locked chain");
            }
#endif
            if (proc != null) { return Add(proc); }
            proc = new P();
            return Add(proc);
        }

        public P Insert<P>(int atIndex, P proc)
            where P : class, IProcessor
        {
#if UNITY_EDITOR
            if (_locked)
            {
                throw new Exception("You cannot insert new processors to a locked chain");
            }
#endif
            if (_childs.Contains(proc))
                return proc;

            if (atIndex > _childs.Count - 1)
                return Add(proc);

            _childs.Insert(atIndex, proc);
            return OnChildAdded(proc, atIndex) as P;
        }

        public P InsertBefore<P>(IProcessor beforeItem, P proc)
            where P : class, IProcessor
        {
#if UNITY_EDITOR
            if (_locked)
            {
                throw new Exception("You cannot insert new processors to a locked chain");
            }
#endif
            int atIndex = _childs.IndexOf(beforeItem);
            if (atIndex == -1) { return Add(proc); }
            return Insert(atIndex, proc);
        }

        public P InsertBefore<P>(IProcessor beforeProc, ref P proc)
            where P : class, IProcessor, new()
        {
#if UNITY_EDITOR
            if (_locked)
            {
                throw new Exception("You cannot insert new processors to a locked chain");
            }
#endif
            if (proc != null) { return InsertBefore(beforeProc, proc); }
            proc = new P();
            return InsertBefore(beforeProc, proc);
        }

        /// <summary>
        /// Removes a processor from the chain
        /// </summary>
        /// <param name="proc"></param>
        public IProcessor Remove(IProcessor proc)
        {
#if UNITY_EDITOR
            if (_locked)
            {
                throw new Exception("You cannot remove processors from a locked chain");
            }
#endif

            int index = _childs.IndexOf(proc);
            if (index == -1) { return null; }

            _childs.RemoveAt(index);
            return OnChildRemoved(proc, index);
        }

        /// <summary>
        /// Removes a processor from the chain
        /// </summary>
        /// <param name="proc"></param>
        public IProcessor RemoveAt(int index)
        {
#if UNITY_EDITOR
            if (_locked)
            {
                throw new Exception("You cannot remove processors from a locked chain");
            }
#endif
            IProcessor proc = _childs[index];
            _childs.RemoveAt(index);
            return OnChildRemoved(proc, index);
        }


        protected IProcessor OnChildAdded(IProcessor child, int childIndex)
        {
            child.compound = this;
            RefreshChildIndices(childIndex);
            return child;
        }

        protected IProcessor OnChildRemoved(IProcessor child, int childIndex)
        {
            child.compound = null;
            RefreshChildIndices(childIndex);
            return child;
        }

        protected void RefreshChildIndices(int from)
        {
            int count = Count;
            if (from <= count - 1)
            {
                for (int i = from; i < count; i++)
                    _childs[i].compoundIndex = i;
            }
        }

        #endregion

        #region Scheduling

        internal override void OnPrepare()
        {
            Prepare(_scaledLockedDelta);
        }

        internal JobHandle ScheduleEmpty(IProcessor dependsOn = null)
        {
            return dependsOn == null
                ? _emptyCompoundJob.Schedule()
                : _emptyCompoundJob.Schedule(dependsOn.currentHandle);
        }

        internal JobHandle ScheduleEmpty(JobHandle dependsOn)
        {
            return _emptyCompoundJob.Schedule(dependsOn);
        }

        /// <summary>
        /// In a ProcessorGroup, Prepare is called right before scheduling the existing group for the job.
        /// If you intend to dynamically modify the group childs list, do so in InternalLock(), right before base.InternalLock().
        /// </summary>
        /// <param name="delta"></param>
        protected virtual void Prepare(float delta) { }

        #endregion

        #region Complete & Apply

        protected sealed override void OnCompleteBegins()
        {
            if (_isCompoundEmpty)
            {
                _currentHandle.Complete();
            }
            else
            {
                for (int i = 0, n = _childs.Count; i < n; i++)
                    _childs[i].Complete();
            }

            Apply();
        }

        protected virtual void Apply() { }

        #endregion

        #region ILockable

        public override sealed void Lock()
        {

            if (_locked) { return; }

            base.Lock();

            _enabledChildren = 0;

            for (int i = 0, n = _childs.Count; i < n; i++)
            {
                IProcessor child = _childs[i];
                child.Lock();

                if (!child.enabled) { continue; }

                // Skip empty compounds
                if (child is IProcessorCompound childCompound && childCompound.isCompoundEmpty)
                {
                    continue;
                }

                _enabledChildren++;
            }

            if (_enabledChildren == 0)
            {
                _isCompoundEmpty = true;
                _emptyCompoundJob = default;
            }
            else
            {
                _isCompoundEmpty = false;
            }

        }

        public override sealed void Unlock()
        {
            if (!_locked) { return; }

            base.Unlock();

            for (int i = 0, n = _childs.Count; i < n; i++)
                _childs[i].Unlock();
        }

        #endregion

        #region Hierarchy

        public bool TryGetFirst<P>(int startIndex, out P processor, bool deep = false)
            where P : class, IProcessor
        {
            if (startIndex < 0) { startIndex = _childs.Count - 1; }

            IProcessor child;
            IProcessorCompound childCompound;

            for (int i = startIndex; i >= 0; i--)
            {
                child = _childs[i];
                processor = child as P;

                if (processor != null)
                {
                    return true;
                }

                if (!deep) { continue; }

                childCompound = child as IProcessorCompound;

                if (childCompound != null
                    && childCompound.Find(out processor))
                    return true;
            }

            //If local search fails, it goes up one level and restarts.
            //This is actually super slow so make sure to cache the results of TryGet & Find.
            return TryGetFirstInCompound(out processor, deep);
        }

        /// <summary>
        /// Goes through all child processors & compounts in reverse order
        /// until if find a processor with the correct signature.
        /// </summary>
        /// <typeparam name="P"></typeparam>
        /// <param name="processor"></param>
        /// <returns></returns>
        public bool Find<P>(out P processor)
            where P : class, IProcessor
        {
            processor = null;

            IProcessor child;
            IProcessorCompound childCompound;

            for (int i = Count - 1; i >= 0; i--)
            {
                child = _childs[i];
                processor = child as P;

                if (processor != null)
                    return true;

                childCompound = child as IProcessorCompound;

                if (childCompound != null
                    && childCompound.Find(out processor))
                    return true;
            }

            return false;
        }

        #endregion

        #region IDisposable

        public void DisposeAll()
        {
#if UNITY_EDITOR
            if (_disposed)
            {
                return;

                //throw new Exception("DisposeAll() called on already disposed Compound.");
            }
#endif

            if (_scheduled) { _currentHandle.Complete(); }

            IProcessor p;

            for (int i = 0, count = _childs.Count; i < count; i++)
            {
                p = _childs[i];

                if (p is IProcessorCompound)
                    (p as IProcessorCompound).DisposeAll();
                else
                    p.Dispose();

            }

            _scheduled = false; // Avoid Completting current handle twice

            Dispose();
        }

        #endregion
    }
}
