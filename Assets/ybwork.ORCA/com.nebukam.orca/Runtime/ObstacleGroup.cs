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

using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace Nebukam.ORCA
{
    /// <summary>
    /// Interface for an ObstacleGroup. Only expose the methods required by the simulation.
    /// </summary>
    public interface IObstacleGroup
    {
        /// <summary>
        /// Number of Obstacle currently in the group.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Retreive the obstacle at a given index
        /// </summary>
        /// <param name="i">Index of the obstacle to be retreived</param>
        /// <returns></returns>
        Obstacle this[int i] { get; }
    }

    /// <summary>
    /// A group of obstacle definitions to be used within an ORCA simulation.
    /// An ObstacleGroup can be used by multiple simulation simultaneously.
    /// </summary>
    public class ObstacleGroup : IObstacleGroup
    {
        protected Pool.OnItemReleased _onObstacleReleased;

        public ObstacleGroup()
        {
            _onObstacleReleased = OnObstacleReleased;
        }

        //Store obsctacles
        protected List<Obstacle> _obstacles = new List<Obstacle>();

        /// <summary>
        /// Number of Obstacle currently in the group.
        /// </summary>
        public int Count { get { return _obstacles.Count; } }

        /// <summary>
        /// Retreive the obstacle at a given index
        /// </summary>
        /// <param name="i">Index of the obstacle to be retreived</param>
        /// <returns></returns>
        public Obstacle this[int i] { get { return _obstacles[i]; } }

        /// <summary>
        /// Adds an obstacle to the group.
        /// </summary>
        /// <param name="obstacle">Obstacle to be added</param>
        /// <returns>Added obstacle</returns>
        public Obstacle Add(Obstacle obstacle)
        {
            if (_obstacles.Contains(obstacle)) { return obstacle; }
            _obstacles.Add(obstacle);
            obstacle.onRelease(_onObstacleReleased);
            return obstacle;
        }

        /// <summary>
        /// Add an obstacle to the group, in the form of a list of vertices.
        /// </summary>
        /// <param name="vertices">A list of vertices</param>
        /// <param name="maxSegmentLength">If > 0.0f, will subdivide segments larger than this threshold.</param>
        /// <returns>The newly created Obstacle</returns>
        public Obstacle Add(IEnumerable<float2> vertices, bool isReverse = false, float maxSegmentLength = 10.0f)
        {
            Obstacle obstacle = Pool.Rent<Obstacle>();

            if (isReverse)
                vertices = vertices.Reverse();

            foreach (float2 vertex in vertices)
                obstacle.Add(vertex);

            if (math.distancesq(obstacle.vertices.Last().pos, obstacle.vertices.First().pos) != 0.0f)
                obstacle.Add(obstacle.vertices.First().pos); // Close obstacle

            if (maxSegmentLength > 0.0f)
                obstacle.Subdivide(maxSegmentLength);

            return Add(obstacle);
        }

        /// <summary>
        /// Removes an Obstacle from the group
        /// </summary>
        /// <param name="obstacle">the obstacle to be removed</param>
        public void Remove(Obstacle obstacle)
        {
            _obstacles.Remove(obstacle);
        }

        protected void OnObstacleReleased(IPoolItem obstacle)
        {
            Remove(obstacle as Obstacle);
        }

        /// <summary>
        /// Clear the group obstacle list and optionally releases each Obstacle it contained.
        /// </summary>
        /// <param name="release">Whether to release individual Obstacles or not</param>
        public void Clear(bool release = false)
        {
            if (release)
            {
                int count = _obstacles.Count;
                while (count != 0)
                {
                    _obstacles[count - 1].Release();
                    count = _obstacles.Count;
                }
            }

            _obstacles.Clear();
        }
    }
}
