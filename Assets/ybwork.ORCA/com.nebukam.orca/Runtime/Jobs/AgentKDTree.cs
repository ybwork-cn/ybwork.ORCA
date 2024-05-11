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
using static Nebukam.JobAssist.Extensions;
using Unity.Collections;
using Unity.Burst;

namespace Nebukam.ORCA
{
    [BurstCompile]
    public struct AgentTreeNode
    {
        public const int MAX_LEAF_SIZE = 10;

        public int begin;
        public int end;
        public int left;
        public int right;
        public float maxX;
        public float maxY;
        public float minX;
        public float minY;
    }

    public interface IAgentKDTreeProvider : IProcessor
    {
        NativeArray<AgentTreeNode> outputTree { get; }
    }

    [BurstCompile]
    public class AgentKDTree<TAgent> : Processor<AgentKDTreeJob>, IAgentKDTreeProvider where TAgent : Agent
    {
        protected NativeArray<AgentTreeNode> _outputTree = default;
        public NativeArray<AgentTreeNode> outputTree => _outputTree;

        #region Inputs

        protected bool _inputsDirty = true;

        protected IAgentProvider<TAgent> _agentProvider;
        public IAgentProvider<TAgent> agentProvider { get { return _agentProvider; } }

        #endregion

        protected override void Prepare(ref AgentKDTreeJob job, float delta)
        {
            if (_inputsDirty)
            {
                if (!TryGetFirstInCompound(out _agentProvider))
                {
                    throw new System.Exception("No IAgentProvider in chain !");
                }

                _inputsDirty = false;
            }

            int agentCount = 2 * _agentProvider.outputAgents.Length;

            MakeLength(ref _outputTree, agentCount);

            job.m_inputAgents = _agentProvider.outputAgents;
            job.m_outputTree = _outputTree;
        }

        protected override void InternalDispose()
        {
            _outputTree.Release();
        }
    }
}
