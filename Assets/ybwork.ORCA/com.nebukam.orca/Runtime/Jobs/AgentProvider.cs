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
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using static Nebukam.JobAssist.Extensions;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{
    public interface IAgentProvider<TAgent> : IProcessor where TAgent : Agent
    {
        NativeArray<AgentData> outputAgents { get; }
        List<TAgent> lockedAgents { get; }
        float maxRadius { get; }
    }

    public class AgentProvider<TAgent> : Processor<Unemployed>, IAgentProvider<TAgent> where TAgent : Agent, new()
    {
        protected AgentGroup<TAgent> _agents = null;
        public AgentGroup<TAgent> agents
        {
            get { return _agents; }
            set { _agents = value; }
        }

        internal List<TAgent> m_lockedAgents = new List<TAgent>();
        public List<TAgent> lockedAgents { get { return m_lockedAgents; } }

        protected NativeArray<AgentData> _outputAgents = default;
        public NativeArray<AgentData> outputAgents { get { return _outputAgents; } }

        protected float _maxRadius = 0f;
        public float maxRadius { get { return _maxRadius; } }

        protected override void InternalLock()
        {
            int count = _agents == null ? 0 : _agents.Count;

            m_lockedAgents.Clear();
            m_lockedAgents.Capacity = math.ceilpow2(count);

            for (int i = 0; i < count; i++)
            {
                m_lockedAgents.Add(_agents[i]);
            }
        }

        protected override void Prepare(ref Unemployed job, float delta)
        {
            int agentCount = m_lockedAgents.Count;

            MakeLength(ref _outputAgents, agentCount);

            _maxRadius = 0f;

            for (int i = 0; i < agentCount; i++)
            {
                Agent a = m_lockedAgents[i];
                _maxRadius = max(_maxRadius, a.radius);
                float2 pos = a.pos;
                float2 prefVel = a.m_prefVelocity;
                float2 vel = a.m_velocity;
                _outputAgents[i] = new AgentData()
                {
                    index = i,
                    kdIndex = i,
                    position = float2(pos.x, pos.y), //
                    worldPosition = pos,
                    prefVelocity = float2(prefVel.x, prefVel.y),
                    velocity = float2(vel.x, vel.y),
                    worldVelocity = vel,
                    radius = a.m_radius,
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
            _agents = null;

            m_lockedAgents.Clear();
            m_lockedAgents = null;

            _outputAgents.Release();
        }
    }
}
