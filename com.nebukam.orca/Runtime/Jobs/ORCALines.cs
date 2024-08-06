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

        protected IStaticObstacleProvider _staticObstaclesProvider;
        protected IStaticObstacleKDTreeProvider _staticObstacleKDTreeProvider;

        protected IDynObstacleProvider _dynObstaclesProvider;
        protected IDynObstacleKDTreeProvider _dynObstacleKDTreeProvider;
        #endregion

        protected override int Prepare(ref ORCALinesJob job, float delta)
        {
            if (_inputsDirty)
            {
                if (!TryGetFirstInCompound(out _agentProvider, true)
                    || !TryGetFirstInCompound(out _agentKDTreeProvider, true)
                    || !TryGetFirstInCompound(out _staticObstaclesProvider, true)
                    || !TryGetFirstInCompound(out _staticObstacleKDTreeProvider, true)
                    || !TryGetFirstInCompound(out _dynObstaclesProvider, true)
                    || !TryGetFirstInCompound(out _dynObstacleKDTreeProvider, true))
                {
                    string msg = string.Format("Missing provider : Agents = {0}, Agent KD = {1}, Static obs = {2}, Static obs KD= {3}, Dynamic obs = {4}, Dynamic obs KD= {5}, group = {6}",
                        _agentProvider,
                        _agentKDTreeProvider,
                        _staticObstaclesProvider,
                        _staticObstacleKDTreeProvider,
                        _dynObstaclesProvider,
                        _dynObstacleKDTreeProvider,
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

            //Static dynamic data
            job.m_dynObstacleInfos = _dynObstaclesProvider.outputObstacleInfos;
            job.m_dynRefObstacles = _dynObstaclesProvider.referenceObstacles;
            job.m_dynObstacles = _dynObstaclesProvider.outputObstacles;
            job.m_dynObstacleTree = _dynObstacleKDTreeProvider.outputTree;

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
