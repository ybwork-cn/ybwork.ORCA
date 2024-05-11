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

using Nebukam.Common;
using Unity.Mathematics;

namespace Nebukam.ORCA
{
    /// <summary>
    /// A group of agent to be used within an ORCA simulation.
    /// An AgentGroup should not be used by multiple simulations simultaneously.
    /// </summary>
    /// <typeparam name="V">Agent Type</typeparam>
    public class AgentGroup<V> : VertexGroup<V>
        where V : Agent, new()
    {
        protected override void OnVertexAdded(V v)
        {
            base.OnVertexAdded(v);
            v.onRelease(_onVertexReleasedCached);
        }

        protected override void OnVertexRemoved(V v)
        {
            base.OnVertexRemoved(v);
            v.offRelease(_onVertexReleasedCached);
        }

        public T Add<T>(float2 pos) where T : V, new()
        {
            T vert = Pool.Rent<T>();
            vert.pos = pos;
            return (T)Add(vert);
        }
    }
}
