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

using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Nebukam.JobAssist.Extensions;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{
    public struct DVP
    {
        public float distSq;
        public int index;
        public DVP(float dist, int i)
        {
            distSq = dist;
            index = i;
        }
    }

    public struct ORCALine
    {
        public float2 dir;
        public float2 point;
    }

    [BurstCompile]
    public struct ORCALinesJob : IJobParallelFor
    {
        const float EPSILON = 0.00001f;

        [ReadOnly]
        public NativeArray<AgentData> m_inputAgents;
        [ReadOnly]
        public NativeArray<AgentTreeNode> m_inputAgentTree;

        [ReadOnly]
        public NativeArray<ObstacleInfos> m_staticObstacleInfos;
        [ReadOnly]
        public NativeArray<ObstacleVertexData> m_staticRefObstacles;
        [ReadOnly]
        public NativeArray<ObstacleVertexData> m_staticObstacles;
        [ReadOnly]
        public NativeArray<ObstacleTreeNode> m_staticObstacleTree;

        [ReadOnly]
        public NativeArray<ObstacleInfos> m_dynObstacleInfos;
        [ReadOnly]
        public NativeArray<ObstacleVertexData> m_dynRefObstacles;
        [ReadOnly]
        public NativeArray<ObstacleVertexData> m_dynObstacles;
        [ReadOnly]
        public NativeArray<ObstacleTreeNode> m_dynObstacleTree;

        public NativeArray<AgentDataResult> m_results;
        public float m_timestep;

        public void Execute(int index)
        {
            AgentData agent = m_inputAgents[index];
            AgentDataResult result = new AgentDataResult();

            if (agent.maxNeighbors == 0 || !agent.navigationEnabled)
            {
                result.position = agent.position;
                result.velocity = agent.velocity;
                m_results[index] = result;
                return;
            }

            float2 a_position = agent.position;
            float2 a_prefVelocity = agent.prefVelocity;
            float2 a_velocity = agent.velocity;
            float2 a_newVelocity = a_prefVelocity;

            float a_maxSpeed = agent.maxSpeed;
            float a_radius = agent.radius;
            float a_radiusObst = agent.radiusObst;
            float a_timeHorizon = agent.timeHorizon;
            float a_timeHorizonObst = agent.timeHorizonObst;
            float obsRangeSq = lengthsq(a_timeHorizonObst * a_maxSpeed + a_radius);
            float rangeSq = lengthsq(agent.radius + agent.neighborDist);

            NativeList<ORCALine> m_orcaLines = new NativeList<ORCALine>(16, Allocator.Temp);

            #region obstacles

            float invTimeHorizonObst = 1.0f / agent.timeHorizonObst;

            #region static obstacles

            if (m_staticObstacleTree.Length > 0)
            {
                NativeList<DVP> staticObstacleNeighbors = new NativeList<DVP>(10, Allocator.Temp);

                QueryObstacleTreeRecursive(
                    ref a_position,
                    ref agent,
                    ref obsRangeSq, 0,
                    ref staticObstacleNeighbors,
                    ref m_staticObstacles,
                    ref m_staticRefObstacles,
                    ref m_staticObstacleInfos,
                    ref m_staticObstacleTree);

                for (int i = 0; i < staticObstacleNeighbors.Length; ++i)
                {
                    ObstacleVertexData vertex = m_staticObstacles[staticObstacleNeighbors[i].index];
                    ObstacleVertexData nextVertex = m_staticRefObstacles[vertex.next];
                    ObstacleInfos infos = m_staticObstacleInfos[vertex.infos];

                    //if(a_top < infos.baseline || a_bottom > infos.baseline + infos.height) { continue; }

                    float2 relPos1 = vertex.pos - a_position;
                    float2 relPos2 = nextVertex.pos - a_position;

                    float oRadius = a_radiusObst + infos.thickness;

                    // Check if velocity obstacle of obstacle is already taken care
                    // of by previously constructed obstacle ORCA lines.
                    bool alreadyCovered = false;

                    for (int j = 0; j < m_orcaLines.Length; ++j)
                    {
                        if (Det(invTimeHorizonObst * relPos1 - m_orcaLines[j].point, m_orcaLines[j].dir) - invTimeHorizonObst * oRadius
                            >= -EPSILON && Det(invTimeHorizonObst * relPos2 - m_orcaLines[j].point, m_orcaLines[j].dir) - invTimeHorizonObst * oRadius >= -EPSILON)
                        {
                            alreadyCovered = true;
                            break;
                        }
                    }

                    if (alreadyCovered)
                        continue;

                    float r = a_radius + a_radiusObst;
                    ORCALine line;

                    // 当前线段法线方向
                    float2 obstacleNormal = new float2(vertex.dir.y, -vertex.dir.x);

                    if (DistSqPointLineSegment(vertex.pos, nextVertex.pos, a_position) < r)
                    {
                        line.point = relPos1 + obstacleNormal * r;
                        line.dir = -vertex.dir;
                        m_orcaLines.Add(line);
                        continue;
                    }

                    if (vertex.convex)
                    {
                        // 移速方向在原点和障碍物线段两端组成的夹角之外
                        var cos1 = dot(normalize(a_velocity), normalize(relPos1));
                        var cos2 = dot(normalize(a_velocity), normalize(relPos2));
                        var cosRange = dot(normalize(relPos1), normalize(relPos2));
                        if (cos1 < cosRange || cos2 < cosRange)
                        {
                            // 移速方向在原点和(障碍物线段扩展半径之后的形状最外侧)组成的夹角之外
                            if (lengthsq(cos1) * lengthsq(relPos1) < lengthsq(relPos1) - lengthsq(r))
                                continue;
                            if (lengthsq(cos2) * lengthsq(relPos2) < lengthsq(relPos2) - lengthsq(r))
                                continue;
                        }
                    }

                    line.point = relPos1 + obstacleNormal * r;
                    line.dir = -vertex.dir;
                    m_orcaLines.Add(line);
                }

                staticObstacleNeighbors.Release();
            }

            #endregion

            #region dynamic obstacles

            if (m_dynObstacleTree.Length > 0)
            {
                NativeList<DVP> dynObstacleNeighbors = new NativeList<DVP>(10, Allocator.Temp);

                QueryObstacleTreeRecursive(
                    ref a_position,
                    ref agent,
                    ref obsRangeSq, 0,
                    ref dynObstacleNeighbors,
                    ref m_dynObstacles,
                    ref m_dynRefObstacles,
                    ref m_dynObstacleInfos,
                    ref m_dynObstacleTree);

                for (int i = 0; i < dynObstacleNeighbors.Length; ++i)
                {
                    ObstacleVertexData vertex = m_dynObstacles[dynObstacleNeighbors[i].index];
                    ObstacleVertexData nextVertex = m_dynRefObstacles[vertex.next];
                    ObstacleInfos infos = m_dynObstacleInfos[vertex.infos];

                    //if(a_top < infos.baseline || a_bottom > infos.baseline + infos.height) { continue; }

                    float2 relPos1 = vertex.pos - a_position;
                    float2 relPos2 = nextVertex.pos - a_position;

                    float oRadius = a_radiusObst + infos.thickness;

                    // Check if velocity obstacle of obstacle is already taken care
                    // of by previously constructed obstacle ORCA lines.
                    bool alreadyCovered = false;

                    for (int j = 0; j < m_orcaLines.Length; ++j)
                    {
                        if (Det(invTimeHorizonObst * relPos1 - m_orcaLines[j].point, m_orcaLines[j].dir) - invTimeHorizonObst * oRadius
                            >= -EPSILON && Det(invTimeHorizonObst * relPos2 - m_orcaLines[j].point, m_orcaLines[j].dir) - invTimeHorizonObst * oRadius >= -EPSILON)
                        {
                            alreadyCovered = true;
                            break;
                        }
                    }

                    if (alreadyCovered)
                        continue;

                    float r = a_radius + a_radiusObst;
                    ORCALine line;

                    // 当前线段法线方向
                    float2 obstacleNormal = new float2(vertex.dir.y, -vertex.dir.x);

                    if (DistSqPointLineSegment(vertex.pos, nextVertex.pos, a_position) < r)
                    {
                        line.point = relPos1 + obstacleNormal * r;
                        line.dir = -vertex.dir;
                        m_orcaLines.Add(line);
                        continue;
                    }

                    if (vertex.convex)
                    {
                        // 移速方向在原点和障碍物线段两端组成的夹角之外
                        var cos1 = dot(normalize(a_velocity), normalize(relPos1));
                        var cos2 = dot(normalize(a_velocity), normalize(relPos2));
                        var cosRange = dot(normalize(relPos1), normalize(relPos2));
                        if (cos1 < cosRange || cos2 < cosRange)
                        {
                            // 移速方向在原点和(障碍物线段扩展半径之后的形状最外侧)组成的夹角之外
                            if (lengthsq(cos1) * lengthsq(relPos1) < lengthsq(relPos1) - lengthsq(r))
                                continue;
                            if (lengthsq(cos2) * lengthsq(relPos2) < lengthsq(relPos2) - lengthsq(r))
                                continue;
                        }
                    }

                    line.point = relPos1 + obstacleNormal * r;
                    line.dir = -vertex.dir;
                    m_orcaLines.Add(line);
                }

                dynObstacleNeighbors.Release();
            }

            #endregion

            int numObstLines = m_orcaLines.Length;

            #endregion

            #region agents

            NativeList<DVP> agentNeighbors = new NativeList<DVP>(agent.maxNeighbors, Allocator.Temp);

            QueryAgentTreeRecursive(
                ref a_position,
                ref agent,
                ref rangeSq, 0,
                ref agentNeighbors);

            float invTimeHorizon = 1.0f / a_timeHorizon;

            for (int i = 0; i < agentNeighbors.Length; ++i)
            {
                AgentData otherAgent = m_inputAgents[agentNeighbors[i].index];

                float2 relPos = otherAgent.position - a_position;
                float2 relVel = a_velocity - otherAgent.velocity;
                float distSq = lengthsq(relPos);
                float cRad = a_radius + otherAgent.radius;
                float cRadSq = lengthsq(cRad);

                ORCALine line = new();
                float2 u;

                if (distSq > cRadSq)
                {
                    // No collision.
                    float2 w = relVel - invTimeHorizon * relPos;

                    // Vector from cutoff center to relative velocity.
                    float wLengthSq = lengthsq(w);
                    float dotProduct1 = dot(w, relPos);

                    if (dotProduct1 < 0.0f && lengthsq(dotProduct1) > cRadSq * wLengthSq)
                    {
                        // Project on cut-off circle.
                        float wLength = sqrt(wLengthSq);
                        float2 unitW = w / wLength;

                        line.dir = float2(unitW.y, -unitW.x);
                        u = (cRad * invTimeHorizon - wLength) * unitW;
                    }
                    else
                    {
                        // Project on legs.
                        float leg = sqrt(distSq - cRadSq);

                        if (Det(relPos, w) > 0.0f)
                        {
                            // Project on left leg.
                            line.dir = float2(relPos.x * leg - relPos.y * cRad, relPos.x * cRad + relPos.y * leg) / distSq;
                        }
                        else
                        {
                            // Project on right leg.
                            line.dir = -float2(relPos.x * leg + relPos.y * cRad, -relPos.x * cRad + relPos.y * leg) / distSq;
                        }

                        float dotProduct2 = dot(relVel, line.dir);
                        u = dotProduct2 * line.dir - relVel;
                    }
                }
                else
                {
                    // Collision. Project on cut-off circle of time timeStep.
                    float invTimeStep = 1.0f / m_timestep;

                    // Vector from cutoff center to relative velocity.
                    float2 w = relVel - invTimeStep * relPos;

                    float wLength = length(w);
                    float2 unitW = w / wLength;

                    line.dir = float2(unitW.y, -unitW.x);
                    u = (cRad * invTimeStep - wLength) * unitW;
                }

                line.point = a_velocity + 0.5f * u;
                m_orcaLines.Add(line);
            }

            agentNeighbors.Release();

            #endregion

            #region Compute new velocity

            int lineFail = LP2(m_orcaLines, a_maxSpeed, a_prefVelocity, false, ref a_newVelocity);

            if (lineFail < m_orcaLines.Length)
                LP3(m_orcaLines, numObstLines, lineFail, a_maxSpeed, ref a_newVelocity);

            #endregion

            result.velocity = a_newVelocity;
            result.position = a_position + a_newVelocity * m_timestep;

            m_results[index] = result;

            m_orcaLines.Release();
        }

        #region Agent KDTree Query

        /// <summary>
        /// 查询所有相邻Agent
        /// Recursive method for computing the agent neighbors of the specified agent.
        /// </summary>
        /// <param name="agent">The agent for which agent neighbors are to be computed.</param>
        /// <param name="agent">The agent making the initial query</param>
        /// <param name="rangeSq">The squared range around the agent.</param>
        /// <param name="node">The current agent k-D tree node index.</param>
        /// <param name="agentNeighbors">The list of neighbors to be filled up.</param>
        private void QueryAgentTreeRecursive(ref float2 center, ref AgentData agent, ref float rangeSq, int node, ref NativeList<DVP> agentNeighbors)
        {
            AgentTreeNode treeNode = m_inputAgentTree[node];

            if (treeNode.end - treeNode.begin <= AgentTreeNode.MAX_LEAF_SIZE)
            {
                for (int i = treeNode.begin; i < treeNode.end; ++i)
                {
                    AgentData a = m_inputAgents[i];

                    if (a.index == agent.index
                        || !a.collisionEnabled
                        // 不同碰撞层的单位不发生碰撞
                        || ((agent.layerOccupation & ~a.layerIgnore) & a.layerOccupation) == ORCALayer.NONE)
                    {
                        continue;
                    }

                    float distSq = lengthsq(center - a.position);

                    if (distSq < rangeSq)
                    {
                        if (agentNeighbors.Length < agent.maxNeighbors)
                        {
                            agentNeighbors.Add(new DVP(distSq, i));
                        }

                        int j = agentNeighbors.Length - 1;

                        while (j != 0 && distSq < agentNeighbors[j - 1].distSq)
                        {
                            agentNeighbors[j] = agentNeighbors[j - 1];
                            --j;
                        }

                        agentNeighbors[j] = new DVP(distSq, i);

                        if (agentNeighbors.Length == agent.maxNeighbors)
                        {
                            rangeSq = agentNeighbors[^1].distSq;
                        }
                    }
                }
            }
            else
            {
                AgentTreeNode leftNode = m_inputAgentTree[treeNode.left], rightNode = m_inputAgentTree[treeNode.right];

                float distSqLeft = lengthsq(max(0.0f, leftNode.minX - center.x))
                    + lengthsq(max(0.0f, center.x - leftNode.maxX))
                    + lengthsq(max(0.0f, leftNode.minY - center.y))
                    + lengthsq(max(0.0f, center.y - leftNode.maxY));
                float distSqRight = lengthsq(max(0.0f, rightNode.minX - center.x))
                    + lengthsq(max(0.0f, center.x - rightNode.maxX))
                    + lengthsq(max(0.0f, rightNode.minY - center.y))
                    + lengthsq(max(0.0f, center.y - rightNode.maxY));

                if (distSqLeft < distSqRight)
                {
                    if (distSqLeft < rangeSq)
                    {
                        QueryAgentTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.left, ref agentNeighbors);

                        if (distSqRight < rangeSq)
                        {
                            QueryAgentTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.right, ref agentNeighbors);
                        }
                    }
                }
                else
                {
                    if (distSqRight < rangeSq)
                    {
                        QueryAgentTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.right, ref agentNeighbors);

                        if (distSqLeft < rangeSq)
                        {
                            QueryAgentTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.left, ref agentNeighbors);
                        }
                    }
                }
            }
        }

        #endregion

        #region Obstacle KDTree Query

        private static void QueryObstacleTreeRecursive(
            ref float2 center,
            ref AgentData agent,
            ref float rangeSq,
            int node,
            ref NativeList<DVP> obstacleNeighbors,
            ref NativeArray<ObstacleVertexData> obstacles,
            ref NativeArray<ObstacleVertexData> refObstacles,
            ref NativeArray<ObstacleInfos> obstaclesInfos,
            ref NativeArray<ObstacleTreeNode> kdTree)
        {
            ObstacleTreeNode treeNode = kdTree[node];

            if (treeNode.end - treeNode.begin <= ObstacleTreeNode.MAX_LEAF_SIZE)
            {
                ObstacleVertexData o, next;
                ObstacleInfos infos;
                for (int i = treeNode.begin; i < treeNode.end; ++i)
                {
                    o = obstacles[i];
                    infos = obstaclesInfos[o.infos];

                    if (!infos.collisionEnabled || (infos.layerOccupation & ~agent.layerIgnore) == 0)
                        continue;

                    next = refObstacles[o.next];
                    float distSq = DistSqPointLineSegment(o.pos, next.pos, center);

                    if (distSq < rangeSq)
                    {
                        float agentLeftOfLine = LeftOf(o.pos, next.pos, center);
                        float distSqLine = lengthsq(agentLeftOfLine) / lengthsq(next.pos - o.pos);

                        if (distSqLine < rangeSq)
                        {
                            if (agentLeftOfLine < 0.0f)
                            {
                                // Try obstacle at this node only if agent is on right side of
                                // obstacle (and can see obstacle).
                                obstacleNeighbors.Add(new DVP(distSq, i));

                                int index = obstacleNeighbors.Length - 1;

                                //re-order to keep the closest vertex first
                                while (index != 0 && distSq < obstacleNeighbors[index - 1].distSq)
                                {
                                    obstacleNeighbors[index] = obstacleNeighbors[index - 1];
                                    --index;
                                }

                                obstacleNeighbors[index] = new DVP(distSq, i);
                            }
                        }
                    }
                }
            }
            else
            {
                ObstacleTreeNode leftNode = kdTree[treeNode.left],
                    rightNode = kdTree[treeNode.right];

                float distSqLeft = lengthsq(max(0.0f, leftNode.minX - center.x))
                    + lengthsq(max(0.0f, center.x - leftNode.maxX))
                    + lengthsq(max(0.0f, leftNode.minY - center.y))
                    + lengthsq(max(0.0f, center.y - leftNode.maxY));
                float distSqRight = lengthsq(max(0.0f, rightNode.minX - center.x))
                    + lengthsq(max(0.0f, center.x - rightNode.maxX))
                    + lengthsq(max(0.0f, rightNode.minY - center.y))
                    + lengthsq(max(0.0f, center.y - rightNode.maxY));

                if (distSqLeft < distSqRight)
                {
                    if (distSqLeft < rangeSq)
                    {
                        QueryObstacleTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.left, ref obstacleNeighbors,
                            ref obstacles, ref refObstacles, ref obstaclesInfos, ref kdTree);

                        if (distSqRight < rangeSq)
                        {
                            QueryObstacleTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.right, ref obstacleNeighbors,
                            ref obstacles, ref refObstacles, ref obstaclesInfos, ref kdTree);
                        }
                    }
                }
                else
                {
                    if (distSqRight < rangeSq)
                    {
                        QueryObstacleTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.right, ref obstacleNeighbors,
                            ref obstacles, ref refObstacles, ref obstaclesInfos, ref kdTree);

                        if (distSqLeft < rangeSq)
                        {
                            QueryObstacleTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.left, ref obstacleNeighbors,
                            ref obstacles, ref refObstacles, ref obstaclesInfos, ref kdTree);
                        }
                    }
                }
            }
        }

        #endregion

        #region Linear programs

        /// <summary>
        /// Solves a one-dimensional linear program on a specified line subject to linear 
        /// constraints defined by lines and a circular constraint.
        /// </summary>
        /// <param name="lines">Lines defining the linear constraints.</param>
        /// <param name="lineNo">The specified line constraint.</param>
        /// <param name="radius">The radius of the circular constraint.</param>
        /// <param name="optVel">The optimization velocity.</param>
        /// <param name="dirOpt">True if the direction should be optimized.</param>
        /// <param name="result">A reference to the result of the linear program.</param>
        /// <returns>True if successful.</returns>
        private static bool LP1(NativeList<ORCALine> lines, int lineNo, float radius, float2 optVel, bool dirOpt, ref float2 result)
        {
            ORCALine line = lines[lineNo];
            float2 dir = line.dir, pt = line.point;

            float dotProduct = dot(pt, dir);
            float discriminant = lengthsq(dotProduct) + lengthsq(radius) - lengthsq(pt);

            if (discriminant < 0.0f)
            {
                // Max speed circle fully invalidates line lineNo.
                return false;
            }

            ORCALine lineA;
            float2 dirA, ptA;

            float sqrtDiscriminant = sqrt(discriminant);
            float tLeft = -dotProduct - sqrtDiscriminant;
            float tRight = -dotProduct + sqrtDiscriminant;

            for (int i = 0; i < lineNo; ++i)
            {

                lineA = lines[i]; dirA = lineA.dir; ptA = lineA.point;

                float denominator = Det(dir, dirA);
                float numerator = Det(dirA, pt - ptA);

                if (abs(denominator) <= EPSILON)
                {
                    // Lines lineNo and i are (almost) parallel.
                    if (numerator < 0.0f)
                    {
                        return false;
                    }

                    continue;
                }

                float t = numerator / denominator;

                if (denominator >= 0.0f)
                {
                    // Line i bounds line lineNo on the right.
                    tRight = min(tRight, t);
                }
                else
                {
                    // Line i bounds line lineNo on the left.
                    tLeft = max(tLeft, t);
                }

                if (tLeft > tRight)
                {
                    return false;
                }
            }

            if (dirOpt)
            {
                // Optimize direction.
                if (dot(optVel, dir) > 0.0f)
                {
                    // Take right extreme.
                    result = pt + tRight * dir;
                }
                else
                {
                    // Take left extreme.
                    result = pt + tLeft * dir;
                }
            }
            else
            {
                // Optimize closest point.
                float t = dot(dir, (optVel - pt));

                if (t < tLeft)
                {
                    result = pt + tLeft * dir;
                }
                else if (t > tRight)
                {
                    result = pt + tRight * dir;
                }
                else
                {
                    result = pt + t * dir;
                }
            }

            return true;
        }

        /// <summary>
        /// Solves a two-dimensional linear program subject to linear 
        /// constraints defined by lines and a circular constraint.
        /// </summary>
        /// <param name="lines">Lines defining the linear constraints.</param>
        /// <param name="radius">The radius of the circular constraint.</param>
        /// <param name="optVel">The optimization velocity.</param>
        /// <param name="dirOpt">True if the direction should be optimized.</param>
        /// <param name="result">A reference to the result of the linear program.</param>
        /// <returns>The number of the line it fails on, and the number of lines if successful.</returns>
        private static int LP2(NativeList<ORCALine> lines, float radius, float2 optVel, bool dirOpt, ref float2 result)
        {
            if (dirOpt)
            {
                // Optimize direction. Note that the optimization velocity is of
                // unit length in this case.
                result = optVel * radius;
            }
            else if (lengthsq(optVel) > (radius * radius))
            {
                // Optimize closest point and outside circle.
                result = normalize(optVel) * radius;
            }
            else
            {
                // Optimize closest point and inside circle.
                result = optVel;
            }

            for (int i = 0, count = lines.Length; i < count; ++i)
            {
                if (Det(lines[i].dir, lines[i].point - result) > 0.0f)
                {
                    // Result does not satisfy constraint i. Compute new optimal result.
                    float2 tempResult = result;
                    if (!LP1(lines, i, radius, optVel, dirOpt, ref result))
                    {
                        result = tempResult;
                        return i;
                    }
                }
            }

            return lines.Length;
        }

        /// <summary>
        /// Solves a two-dimensional linear program subject to linear
        /// constraints defined by lines and a circular constraint.
        /// </summary>
        /// <param name="lines">Lines defining the linear constraints.</param>
        /// <param name="numObstLines">Count of obstacle lines.</param>
        /// <param name="beginLine">The line on which the 2-d linear program failed.</param>
        /// <param name="radius">The radius of the circular constraint.</param>
        /// <param name="result">A reference to the result of the linear program.</param>
        private static void LP3(NativeList<ORCALine> lines, int numObstLines, int beginLine, float radius, ref float2 result)
        {
            float distance = 0.0f;

            ORCALine lineA, lineB;
            float2 dirA, ptA, dirB, ptB;

            for (int i = beginLine, iCount = lines.Length; i < iCount; ++i)
            {
                lineA = lines[i]; dirA = lineA.dir; ptA = lineA.point;

                if (Det(dirA, ptA - result) > distance)
                {
                    // Result does not satisfy constraint of line i.
                    NativeList<ORCALine> projLines = new NativeList<ORCALine>(numObstLines, Allocator.Temp);

                    for (int ii = 0; ii < numObstLines; ++ii)
                    {
                        projLines.Add(lines[ii]);
                    }

                    for (int j = numObstLines; j < i; ++j)
                    {

                        lineB = lines[j]; dirB = lineB.dir; ptB = lineB.point;

                        ORCALine line = new ORCALine();
                        float determinant = Det(dirA, dirB);

                        if (abs(determinant) <= EPSILON)
                        {
                            // Line i and line j are parallel.
                            if (dot(dirA, dirB) > 0.0f)
                            {
                                // Line i and line j point in the same direction.
                                continue;
                            }
                            else
                            {
                                // Line i and line j point in opposite direction.
                                line.point = 0.5f * (ptA + ptB);
                            }
                        }
                        else
                        {
                            line.point = ptA + (Det(dirB, ptA - ptB) / determinant) * dirA;
                        }

                        line.dir = normalize(dirB - dirA);
                        projLines.Add(line);
                    }

                    float2 tempResult = result;
                    if (LP2(projLines, radius, float2(-dirA.y, dirA.x), true, ref result) < projLines.Length)
                    {
                        // This should in principle not happen. The result is by
                        // definition already in the feasible region of this
                        // linear program. If it fails, it is due to small
                        // floating point error, and the current result is kept.
                        result = tempResult;
                    }

                    distance = Det(dirA, ptA - result);

                    //projLines.Dispose(); //Burst doesn't like this.
                }
            }
        }

        #endregion

        #region maths

        /// <summary>
        /// Computes the determinant of a two-dimensional square matrix 
        /// with rows consisting of the specified two-dimensional vectors.
        /// </summary>
        /// <param name="a">The top row of the two-dimensional square matrix</param>
        /// <param name="b">The bottom row of the two-dimensional square matrix</param>
        /// <returns>The determinant of the two-dimensional square matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Det(float2 a, float2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float LeftOf(float2 a, float2 b, float2 c)
        {
            float x1 = a.x - c.x;
            float y1 = a.y - c.y;
            float x2 = b.x - a.x;
            float y2 = b.y - a.y;
            return x1 * y2 - y1 * x2;
        }

        /// <summary>
        /// Computes the squared distance from a line segment with the specified endpoints to a specified point.
        /// 点到线段的最近距离
        /// </summary>
        /// <param name="a">The first endpoint of the line segment.</param>
        /// <param name="b">The second endpoint of the line segment.</param>
        /// <param name="c">The point to which the squared distance is to be calculated.</param>
        /// <returns>The squared distance from the line segment to the point.</returns>
        private static float DistSqPointLineSegment(float2 a, float2 b, float2 c)
        {
            // inline operations instead of calling shorthands
            float2 ca = c - a;
            float2 ba = b - a;
            float dot_ca_ba = ca.x * ba.x + ca.y * ba.y;

            // ac在ab投影长度除以ab长度
            float r = dot_ca_ba / dot(ba, ba);

            if (r < 0.0f)
            {
                return dot(ca, ca);
            }

            if (r > 1.0f)
            {
                float2 cb = c - b;
                return dot(cb, cb);
            }

            float2 d = c - (a + r * ba);
            return dot(d, d);
        }

        #endregion
    }
}
