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

using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.Common;

namespace Nebukam.ORCA
{
    public class Agent : Vertex
    {
        /// 
        /// Fields
        /// 

        protected internal float2 m_targetPos = float2(0);
        protected internal float2 m_prefVelocity = float2(0);
        protected internal float2 m_velocity = float2(0);

        public float radius = 0.5f;
        protected internal float m_radiusObst = 0f;
        protected internal float m_maxSpeed = 20.0f;

        protected internal int m_maxNeighbors = 7;
        protected internal float m_neighborDist = 3.0f;
        protected internal float m_neighborElev = 0.5f;

        protected internal float m_timeHorizon = 5.0f;
        protected internal float m_timeHorizonObst = 1.2f;

        protected internal ORCALayer m_layerOccupation = ORCALayer.ANY;
        protected internal ORCALayer m_layerIgnore = ORCALayer.NONE;
        protected internal ORCALayer m_layerFlag = ORCALayer.NONE;
        protected internal bool m_navigationEnabled = true;
        protected internal bool m_collisionEnabled = true;

        /// 
        /// Properties
        /// 

        public float2 targetPos
        {
            get => m_targetPos;
            set => m_targetPos = value;
        }

        /// <summary>
        /// Preferred velocity of the agent.
        /// This is the 'ideal', desired velocity.
        /// Note : The agent velocity is multiplied by the simulation's timestep.
        /// </summary>
        public float2 prefVelocity
        {
            get { return m_prefVelocity; }
            set { m_prefVelocity = value; }
        }
        /// <summary>
        /// Simulated, collision-free velocity.
        /// </summary>
        public float2 velocity
        {
            get { return m_velocity; }
            set { m_velocity = value; }
        }

        /// <summary>
        /// Radius of the agent when resolving agent-obstacle collisions.
        /// </summary>
        public float radiusObst
        {
            get { return m_radiusObst; }
            set { m_radiusObst = value; }
        }
        /// <summary>
        /// Maximum allowed speed of the agent.
        /// This is used to avoid deadlock situation where a slight
        /// boost in velocity could help solve more complex scenarios.
        /// </summary>
        public float maxSpeed
        {
            get { return m_maxSpeed; }
            set { m_maxSpeed = value; }
        }

        /// <summary>
        /// Maxmimum number of neighbors this agent accounts for in the simulation
        /// </summary>
        public int maxNeighbors
        {
            get { return m_maxNeighbors; }
            set { m_maxNeighbors = value; }
        }
        /// <summary>
        /// Maximum distance at which this agent consider avoiding other agents
        /// </summary>
        public float neighborDist
        {
            get { return m_neighborDist; }
            set { m_neighborDist = value; }
        }

        /// <summary>
        /// Used to modulate distance checks toward other Agents within the simulation.
        /// </summary>
        public float timeHorizon
        {
            get { return m_timeHorizon; }
            set { m_timeHorizon = value; }
        }
        /// <summary>
        /// Used to modulate distance checks toward Obstacles within the simulation.
        /// </summary>
        public float timeHorizonObst
        {
            get { return m_timeHorizonObst; }
            set { m_timeHorizonObst = value; }
        }

        /// <summary>
        /// Layers on which this agent is physically present, and thus will affect
        /// other agents navigation.
        /// </summary>
        public ORCALayer layerOccupation
        {
            get { return m_layerOccupation; }
            set { m_layerOccupation = value; }
        }
        /// <summary>
        /// Ignored layers while resolving the simulation.
        /// </summary>
        public ORCALayer layerIgnore
        {
            get { return m_layerIgnore; }
            set { m_layerIgnore = value; }
        }
        /// <summary>
        /// 层级设置，用于区分Agent的类型(与碰撞无关，用于查找指定类型的临近点)
        /// </summary>
        public ORCALayer layerFlag
        {
            get => m_layerFlag;
            set => m_layerFlag = value;
        }
        /// <summary>
        /// Whether this agent's navigation is controlled by the simulation.
        /// This property has precedence over layers.
        /// </summary>
        public bool navigationEnabled
        {
            get { return m_navigationEnabled; }
            set { m_navigationEnabled = value; }
        }
        /// <summary>
        /// Whether this agent's collision is enabled.
        /// This property has precedence over layers.
        /// </summary>
        public bool collisionEnabled
        {
            get { return m_collisionEnabled; }
            set { m_collisionEnabled = value; }
        }
    }
}
