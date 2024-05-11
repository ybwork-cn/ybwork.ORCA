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

namespace Nebukam.ORCA
{
    /// <summary>
    /// Applies computed simulation result
    /// and update Agent's position and velocity
    /// </summary>
    public class ORCAApply<TAgent> : ParallelProcessor<ORCAApplyJob> where TAgent : Agent
    {
        #region Inputs

        protected bool _inputsDirty = true;

        protected IORCALinesProvider<TAgent> _orcaResultProvider;

        #endregion

        protected override int Prepare(ref ORCAApplyJob job, float delta)
        {
            if (_inputsDirty)
            {

                if (!TryGetFirstInCompound(out _orcaResultProvider, true))
                {
                    throw new System.Exception("IORCALinesProvider missing.");
                }

                _inputsDirty = false;
            }

            job.m_inputAgents = _orcaResultProvider.agentProvider.outputAgents;
            job.m_inputAgentResults = _orcaResultProvider.results;
            job.m_timestep = delta;

            return _orcaResultProvider.results.Length;
        }

        protected override void Apply(ref ORCAApplyJob job)
        {
            IAgentProvider<TAgent> agentProvider = _orcaResultProvider.agentProvider;
            NativeArray<AgentData> agentDataList = agentProvider.outputAgents;
            List<TAgent> agentList = agentProvider.lockedAgents;

            for (int i = 0, count = agentDataList.Length; i < count; i++)
            {
                AgentData agentData = agentDataList[i];
                Agent agent = agentList[agentData.index];
                agent.pos = agentData.worldPosition;
                agent.velocity = agentData.worldVelocity;
                //if(math.length(agent.velocity)> agent.maxSpeed)
            }
        }
    }
}
