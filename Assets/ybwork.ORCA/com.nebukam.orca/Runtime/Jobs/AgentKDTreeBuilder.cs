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

using Nebukam.JobAssist;
using Nebukam.Common;
using Unity.Burst;

namespace Nebukam.ORCA
{

    [BurstCompile]
    public class AgentKDTreeBuilder<TAgent> : ProcessorChain where TAgent : Agent, new()
    {
        protected AgentProvider<TAgent> _agentProvider;
        protected AgentKDTree<TAgent> _agentKDTreeProvider;

        public AgentGroup<TAgent> agents
        {
            get { return _agentProvider.agents; }
            set { _agentProvider.agents = value; }
        }

        public AgentKDTreeBuilder()
        {
            Add(ref _agentProvider);
            Add(ref _agentKDTreeProvider);
        }
    }
}
