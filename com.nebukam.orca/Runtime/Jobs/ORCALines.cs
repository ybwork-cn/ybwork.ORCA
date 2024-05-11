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
using Unity.Collections;
using static Nebukam.JobAssist.Extensions;

namespace Nebukam.ORCA
{
    public interface IORCALinesProvider<TAgent> : IProcessor where TAgent : Agent
    {
        IAgentProvider<TAgent> agentProvider { get; }
        NativeArray<AgentDataResult> results { get; }
    }

    public class ORCALines<TAgent> : ParallelProcessor<ORCALinesJob>, IORCALinesProvider<TAgent> where TAgent : Agent
    {
        protected NativeArray<AgentDataResult> _results = default;
        public NativeArray<AgentDataResult> results { get { return _results; } }

        #region Inputs

        protected bool _inputsDirty = true;

        protected IAgentProvider<TAgent> _agentProvider;
        public IAgentProvider<TAgent> agentProvider { get { return _agentProvider; } }

        protected IAgentKDTreeProvider _agentKDTreeProvider;
        public IAgentKDTreeProvider agentKDTreeProvider { get { return _agentKDTreeProvider; } }

        protected IStaticObstacleProvider _staticObstaclesProvider;
        public IStaticObstacleProvider staticObstaclesProvider { get { return _staticObstaclesProvider; } }

        protected IStaticObstacleKDTreeProvider _staticObstacleKDTreeProvider;
        public IStaticObstacleKDTreeProvider staticObstacleKDTreeProvider { get { return _staticObstacleKDTreeProvider; } }

        #endregion

        protected override int Prepare(ref ORCALinesJob job, float delta)
        {
            if (_inputsDirty)
            {
                if (!TryGetFirstInCompound(out _agentProvider, true)
                    || !TryGetFirstInCompound(out _agentKDTreeProvider, true)
                    || !TryGetFirstInCompound(out _staticObstaclesProvider, true)
                    || !TryGetFirstInCompound(out _staticObstacleKDTreeProvider, true))
                {
                    string msg = string.Format("Missing provider : Agents = {0}, Static obs = {1}, Agent KD = {2}, Static obs KD= {3}, group = {4}",
                        _agentProvider,
                        _staticObstaclesProvider,
                        _agentKDTreeProvider,
                        _staticObstacleKDTreeProvider,
                        _compound);

                    throw new System.Exception(msg);
                }

                _inputsDirty = false;
            }

            int agentCount = _agentProvider.outputAgents.Length;

            MakeLength(ref _results, agentCount);

            //Agent data
            job.m_inputAgents = _agentProvider.outputAgents;
            job.m_inputAgentTree = _agentKDTreeProvider.outputTree;

            //Static obstacles data
            job.m_staticObstacleInfos = _staticObstaclesProvider.outputObstacleInfos;
            job.m_staticRefObstacles = _staticObstaclesProvider.referenceObstacles;
            job.m_staticObstacles = _staticObstaclesProvider.outputObstacles;
            job.m_staticObstacleTree = _staticObstacleKDTreeProvider.outputTree;

            job.m_results = _results;
            job.m_timestep = delta;

            return agentCount;
        }

        protected override void Apply(ref ORCALinesJob job)
        {
            //for (int i = 0; i < job.m_inputAgents.Length; i++)
            //{
            //    int targetIndex = job.QueryAgentTreeRecursive(job.m_inputAgents[i]).index;
            //}
        }

        protected override void InternalDispose()
        {
            _results.Release();
        }
    }
}
