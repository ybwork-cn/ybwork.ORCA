// Copyright (c) 2021 Timothé Lapetite - nebukam@gmail.com
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;

namespace Nebukam
{
    public static class Pool
    {
        public delegate void OnItemReleased(IPoolItem item);
        private static readonly Dictionary<Type, IPool> _pools = new Dictionary<Type, IPool>();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static Pool<T> GetPool<T>(Type type)
            where T : PoolItem, IPoolNode, new()
        {
            Pool<T> pool;
            if (!_pools.TryGetValue(type, out IPool result))
            {
                pool = new Pool<T>();
                _pools.Add(type, pool);
            }
            else
            {
                pool = result as Pool<T>;
            }

            return pool;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Rent<T>()
            where T : PoolItem, new()
        {
            return GetPool<T>(typeof(T)).Rent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        internal static bool ReturnNode(IPoolNode node)
        {
            if (!_pools.TryGetValue(node.GetType(), out IPool pool))
                return false;
            else
                return pool.Return(node);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="this"></param>
        /// <param name="returnDelegate"></param>
        public static void onRelease(this PoolItem @this, Pool.OnItemReleased returnDelegate)
        {
            if ((@this as IPoolNode).__released) { return; }

            List<OnItemReleased> list = @this.__onRelease;

            if (list == null)
            {
                list = new List<OnItemReleased>();
                @this.__onRelease = list;
            }

            if (!list.Contains(returnDelegate))
                list.Add(returnDelegate);

            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="this"></param>
        /// <param name="returnDelegate"></param>
        public static void offRelease(this PoolItem @this, Pool.OnItemReleased returnDelegate)
        {
            if ((@this as IPoolNode).__released) { return; }

            List<OnItemReleased> list = @this.__onRelease;

            if (list == null)
                return;

            if (list.Remove(returnDelegate))
            {
            }

            return;
        }
    }

    internal interface IPool
    {
        int poolSize { get; }
        int newTicker { get; }
        bool Return(IPoolNode node);
    }

    internal class Pool<T> : IPool
        where T : PoolItem, IPoolNode, new()
    {

        protected internal int m_poolSize = 0;
        protected internal int m_newTicker = 0;

        protected internal T m_tail = null;

        int IPool.poolSize { get { return m_poolSize; } }
        int IPool.newTicker { get { return m_newTicker; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="count"></param>
        internal void Preload(int count)
        {

            if (m_tail == null)
            {
                m_tail = new T();
                m_poolSize++;
            }

            T preloaded;
            while (m_poolSize != count)
            {
                preloaded = new T();
                preloaded.__prevNode = m_tail;
                m_tail = preloaded;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal T Rent()
        {

            T item;
            IRequireInit init;

            if (m_tail != null)
            {
                item = m_tail;
                m_tail = m_tail.__prevNode as T;
                item.__prevNode = null;
                item.__released = false;
                m_poolSize--;

                init = item as IRequireInit;
                init?.Init();

                return item;
            }

            item = new T();
            init = item as IRequireInit;
            init?.Init();

            m_newTicker++;
            return item;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        bool IPool.Return(IPoolNode node)
        {
            if (node is not T nAsT)
                return false;
            return Return(nAsT);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        internal bool Return(T node)
        {
            if (node.__released) { return false; }
            node.__released = true;

            List<Pool.OnItemReleased> list = node.__onRelease;
            if (list != null)
            {
                for (int i = 0, count = list.Count; i < count; i++)
                    list[i](node);

                list.Clear();
            }

            if (m_tail != null)
                node.__prevNode = m_tail;

            m_tail = node;

            if (m_tail is IRequireCleanUp clean)
                clean.CleanUp();

            m_poolSize++;
            return true;
        }
    }
}
