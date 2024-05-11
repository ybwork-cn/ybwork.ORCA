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

namespace Nebukam.ORCA
{
    public class ORCA<TAgent> : ProcessorChain where TAgent : Agent, new()
    {
        // Preparation
        protected ObstacleKDTreeBuilder<IStaticObstacleProvider, StaticObstacleProvider, StaticObstacleKDTreeProcessor> _staticObstacles;
        protected AgentKDTreeBuilder<TAgent> _agents;

        public ObstacleGroup staticObstacles => _staticObstacles.obstacles;
        public AgentGroup<TAgent> agents => _agents.agents;

        protected ORCALines<TAgent> _orcaLines;
        protected ORCAApply<TAgent> _orcaApply;

        public ORCA()
        {
            // Preparation
            Add(ref _staticObstacles);
            _staticObstacles.obstacles = new ObstacleGroup();
            Add(ref _agents);
            _agents.agents = new AgentGroup<TAgent>();

            // Execution
            Add(ref _orcaLines);
            _orcaLines.chunkSize = 5; //Linear programs are hefty >.<

            Add(ref _orcaApply);
            _orcaApply.chunkSize = 64;
        }
    }
}
