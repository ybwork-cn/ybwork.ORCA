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
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Nebukam.Common
{
    public interface IVertexGroup<out V>
        where V : Vertex
    {
        int Count { get; }
        V this[int index] { get; }
    }

    public interface IClearableVertexGroup<V> : IVertexGroup<V>
        where V : Vertex
    {
        void Clear(bool release = false);
    }

    public interface IEditableVertexGroup<V> : IClearableVertexGroup<V>
        where V : Vertex
    {
        #region add

        V Add();
        V Add(V v);
        V Add(float2 v);
        V Insert(int index, V v);
        V Insert(int index, float2 v);

        #endregion

        #region remove

        V Remove(V v, bool release = false);
        V RemoveAt(int index, bool release = false);

        #endregion

        #region utils

        void Reverse();
        V Shift(bool release = false);
        V Pop(bool release = false);
        void Offset(float2 offset);

        #endregion
    }

    public class VertexGroup<V> : PoolItem, IEditableVertexGroup<V>
        where V : Vertex, new()
    {
        protected Pool.OnItemReleased _onVertexReleasedCached;

        public bool locked { get; } = false;

        protected List<V> _vertices = new List<V>();
        public List<V> vertices { get { return _vertices; } }

        public int Count { get { return _vertices.Count; } }

        public V this[int index] { get { return _vertices[index]; } }

        public VertexGroup()
        {
            _onVertexReleasedCached = OnVertexReleased;
        }

        #region add

        /// <summary>
        /// Create a vertex in the group
        /// </summary>
        /// <returns></returns>
        public virtual V Add()
        {
            return Add(Pool.Rent<V>());
        }

        /// <summary>
        /// Adds a vertex in the group.
        /// </summary>
        /// <param name="v">The vertex to be added.</param>
        /// <param name="ownVertex">Whether or not this group gets ownership over the vertex.</param>
        /// <returns></returns>
        public V Add(V v)
        {
            if (v is not V vert)
                throw new System.Exception("Wrong vertex type");

            if (_vertices.Contains(vert))
                return vert;

            _vertices.Add(vert);
            OnVertexAdded(vert);
            return vert;
        }

        /// <summary>
        /// Create a vertex in the group, from a float3.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public virtual V Add(float2 v)
        {
            V vert = Pool.Rent<V>();
            vert.pos = v;
            return Add(vert);
        }

        /// <summary>
        /// Inserts a vertex at a given index in the group.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="v"></param>
        /// <param name="ownVertex"></param>
        /// <param name="allowProxy"></param>
        /// <returns></returns>
        public V Insert(int index, V v)
        {
            if (v is null)
                throw new System.Exception("Insert(float, IVertex) : parameter T (" + v.GetType().Name + ") does not implement " + typeof(V).Name + ".");

            V vert = v;

            int currentIndex = _vertices.IndexOf(v);
            if (currentIndex == index)
                return vert;

            if (currentIndex != -1)
            {
                _vertices.RemoveAt(currentIndex);

                if (currentIndex < index)
                    _vertices.Insert(index - 1, vert);
                else
                    _vertices.Insert(index, vert);
            }
            else
            {
                //Add vertex
                _vertices.Insert(index, vert);
                OnVertexAdded(vert);
            }

            return vert;
        }

        /// <summary>
        /// Create a vertex in the group at the given index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public virtual V Insert(int index, float2 v)
        {
            V vert = Pool.Rent<V>();
            vert.pos = v;

            _vertices.Insert(index, vert);
            OnVertexAdded(vert);
            return vert;
        }

        #endregion

        #region remove

        /// <summary>
        /// Removes a given vertex from the group.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="keepProxies"></param>
        /// <returns></returns>
        public V Remove(V v, bool release = false)
        {
            int index = _vertices.IndexOf(v);
            return RemoveAt(index);
        }

        /// <summary>
        /// Removes the vertex at the given index from the group .
        /// </summary>
        /// <param name="index"></param>
        /// <param name="keepProxies"></param>
        /// <returns></returns>
        public V RemoveAt(int index, bool release = false)
        {
            V result = _vertices[index];
            _vertices.RemoveAt(index);
            OnVertexRemoved(result);
            if (release) { result.Release(); }
            return result;
        }

        #endregion

        #region callbacks

        protected virtual void OnVertexAdded(V v)
        {
            //v.OnRelease(m_onVertexReleasedCached);
        }

        protected virtual void OnVertexRemoved(V v)
        {
            //v.OffRelease(m_onVertexReleasedCached);
        }

        protected virtual void OnVertexReleased(IPoolItem vertex)
        {
            Remove(vertex as V);
        }

        #endregion

        #region utils

        /// <summary>
        /// Inverse vertices's order
        /// </summary>
        public void Reverse()
        {
            _vertices.Reverse();
        }

        /// <summary>
        /// Removes and return the first item in the group
        /// </summary>
        /// <returns></returns>
        public V Shift(bool release = false)
        {
            int count = _vertices.Count;
            if (count == 0) { return null; }
            return RemoveAt(0, release);
        }

        /// <summary>
        /// Removes and return the last item in the group
        /// </summary>
        /// <returns></returns>
        public V Pop(bool release = false)
        {
            int count = _vertices.Count;
            if (count == 0) { return null; }
            return RemoveAt(count - 1, release);
        }

        /// <summary>
        /// Removes all vertices from the group.
        /// </summary>
        public virtual void Clear(bool release = false)
        {
            int count = _vertices.Count;
            while (count != 0)
            {
                RemoveAt(count - 1, release);
                count = _vertices.Count;
            }
        }

        /// <summary>
        /// Offset all vertices
        /// </summary>
        /// <param name="offset"></param>
        public void Offset(float2 offset)
        {
            for (int i = 0, count = _vertices.Count; i < count; i++)
                _vertices[i].pos += offset;
        }

        #endregion

        #region PoolItem

        protected virtual void CleanUp()
        {
            Clear(false);
        }

        #endregion

    }
}
