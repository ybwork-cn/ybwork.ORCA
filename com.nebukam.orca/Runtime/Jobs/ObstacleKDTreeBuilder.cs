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
using Nebukam.Common;

namespace Nebukam.ORCA
{

    public class ObstacleKDTreeBuilder<T, P, KD> : ProcessorChain
        where T : class, IProcessor, IObstacleProvider
        where P : class, T, new()
        where KD : ObstacleKDTree<T>, new()
    {
        protected P _obstacleProvider;
        public ObstacleGroup obstacles
        {
            get => _obstacleProvider.obstacles;
            set => _obstacleProvider.obstacles = value;
        }

        protected ObstacleOrientationPass<T> _orientation;
        protected ObstacleFix<T> _fix;
        protected KD _kdTree;

        public ObstacleKDTreeBuilder()
        {
            Add(ref _obstacleProvider); //Create base obstacle structure
            Add(ref _orientation); //Compute obstacle direction & type (convex/concave)
            _orientation.chunkSize = 64;

            Add(ref _fix);
            Add(ref _kdTree); //Compute & split actual KDTree
        }

    }

}
