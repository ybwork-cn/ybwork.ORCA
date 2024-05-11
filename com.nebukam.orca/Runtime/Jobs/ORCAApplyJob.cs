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

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Nebukam.ORCA
{
    [BurstCompile]
    public struct ORCAApplyJob : IJobParallelFor
    {
        [ReadOnly]
        public float m_timestep;

        [ReadOnly]
        public NativeArray<AgentDataResult> m_inputAgentResults;

        public NativeArray<AgentData> m_inputAgents;

        public void Execute(int index)
        {
            AgentDataResult result = m_inputAgentResults[index];
            AgentData agent = m_inputAgents[index];

            float2 worldPosition = result.position;
            float2 worldVelocity = result.velocity;

            agent.position = result.position;
            agent.worldPosition = worldPosition;
            agent.worldVelocity = worldVelocity;
            m_inputAgents[index] = agent;
        }
    }
}
