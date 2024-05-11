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

    public class ObstacleOrientationPass<T> : ParallelProcessor<ObstacleOrientationJob>
        where T : class, IProcessor, IObstacleProvider
    {

        #region Inputs

        protected bool _inputsDirty = true;

        protected T _obstaclesProvider;

        #endregion

        protected override int Prepare(ref ObstacleOrientationJob job, float delta)
        {

            if (_inputsDirty)
            {

                if (!TryGetFirstInCompound(out _obstaclesProvider, true))
                {
                    throw new System.Exception("IObstacleProvider missing.");
                }

                _inputsDirty = false;

            }

            job.m_recompute = _obstaclesProvider.recompute;
            job.m_inputObstacleInfos = _obstaclesProvider.outputObstacleInfos;
            job.m_referenceObstacles = _obstaclesProvider.referenceObstacles;
            job.m_inputObstacles = _obstaclesProvider.outputObstacles;

            return _obstaclesProvider.outputObstacles.Length;

        }

    }
}
