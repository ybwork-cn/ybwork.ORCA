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
    public interface IAgentProvider<TAgent> : IProcessor where TAgent : Agent
    {
        NativeArray<AgentData> outputAgents { get; }
        TAgent[] lockedAgents { get; }
        float maxRadius { get; }
    }

    public class AgentProvider<TAgent> : Processor<Unemployed>, IAgentProvider<TAgent> where TAgent : Agent, new()
    {
        public AgentGroup<TAgent> agents = null;

        internal TAgent[] m_lockedAgents = null;
        public TAgent[] lockedAgents { get { return m_lockedAgents; } }

        protected NativeArray<AgentData> _outputAgents = default;
        public NativeArray<AgentData> outputAgents { get { return _outputAgents; } }

        protected float _maxRadius = 0f;
        public float maxRadius { get { return _maxRadius; } }

        protected override void InternalLock()
        {
            int count = agents == null ? 0 : agents.Count;

            m_lockedAgents = new TAgent[count];
            for (int i = 0; i < count; i++)
            {
                m_lockedAgents[i] = agents[i];
            }
        }

        protected override void Prepare(ref Unemployed job, float delta)
        {
            int agentCount = m_lockedAgents.Length;

            MakeLength(ref _outputAgents, agentCount);

            System.Span<AgentData> outputAgents = _outputAgents.AsSpan();

            _maxRadius = 0f;

            for (int i = 0; i < agentCount; i++)
            {
                Agent a = m_lockedAgents[i];
                if (_maxRadius < a.radius)
                    _maxRadius = a.radius;
                outputAgents[i] = new AgentData()
                {
                    index = i,
                    kdIndex = i,
                    position = a.pos,
                    worldPosition = a.pos,
                    prefVelocity = a.m_prefVelocity,
                    velocity = a.m_velocity,
                    worldVelocity = a.m_velocity,
                    radius = a.radius,
                    radiusObst = a.m_radiusObst,
                    maxSpeed = a.m_maxSpeed,
                    maxNeighbors = a.m_maxNeighbors,
                    neighborDist = a.m_neighborDist,
                    neighborElev = a.m_neighborElev,
                    timeHorizon = a.m_timeHorizon,
                    timeHorizonObst = a.m_timeHorizonObst,
                    navigationEnabled = a.m_navigationEnabled,
                    collisionEnabled = a.m_collisionEnabled,
                    layerOccupation = a.m_layerOccupation,
                    layerIgnore = a.m_layerIgnore,
                    layerFlag = a.m_layerFlag,
                };
            }
        }

        protected override void InternalDispose()
        {
            agents = null;

            m_lockedAgents = null;

            _outputAgents.Release();
        }
    }
}
