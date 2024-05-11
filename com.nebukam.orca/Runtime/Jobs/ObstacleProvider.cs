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
using static Nebukam.JobAssist.Extensions;
using Unity.Collections;
using Unity.Mathematics;

namespace Nebukam.ORCA
{

    public interface IObstacleProvider : IProcessor
    {
        ObstacleGroup obstacles { get; set; }

        bool recompute { get; } //Allows KDTree builders to rebuild or skip rebuild
        NativeArray<ObstacleInfos> outputObstacleInfos { get; }
        NativeArray<ObstacleVertexData> referenceObstacles { get; }
        NativeArray<ObstacleVertexData> outputObstacles { get; }
    }

    public interface IDynObstacleProvider : IObstacleProvider { }
    public interface IStaticObstacleProvider : IObstacleProvider { }

    public class ObstacleProvider : Processor<Unemployed>, IObstacleProvider
    {
        /// 
        /// Fields
        ///

        protected bool _recompute = true;
        protected ObstacleGroup _obstacles = null;
        protected NativeArray<ObstacleInfos> _outputObstacleInfos = default;
        protected NativeArray<ObstacleVertexData> _referenceObstacles = default;
        protected NativeArray<ObstacleVertexData> _outputObstacles = default;


        /// 
        /// Properties
        ///

        public bool recompute { get { return _recompute; } set { _recompute = true; } }
        public ObstacleGroup obstacles
        {
            get { return _obstacles; }
            set { _obstacles = value; _recompute = true; }
        }
        public NativeArray<ObstacleInfos> outputObstacleInfos { get { return _outputObstacleInfos; } }
        public NativeArray<ObstacleVertexData> referenceObstacles { get { return _referenceObstacles; } }
        public NativeArray<ObstacleVertexData> outputObstacles { get { return _outputObstacles; } }

        protected override void InternalLock() { }

        protected override void Prepare(ref Unemployed job, float delta)
        {
            int obsCount = _obstacles == null ? 0 : _obstacles.Count,
             refCount = _referenceObstacles.Length, vCount = 0;

            _recompute = !MakeLength(ref _outputObstacleInfos, obsCount);

            Obstacle o;
            ObstacleInfos infos;

            for (int i = 0; i < obsCount; i++)
            {
                o = _obstacles[i];
                //Keep collision infos & ORCALayer up-to-date
                //there is no need to recompute anything else.
                infos = o.infos;
                infos.index = i;
                infos.start = vCount;
                _outputObstacleInfos[i] = infos;

                vCount += infos.length;
            }

            if (!_recompute)
            {
                if (refCount != vCount)
                {
                    _recompute = true;
                }
                else
                {
                    return;
                }
            }

            MakeLength(ref _referenceObstacles, vCount);
            MakeLength(ref _outputObstacles, vCount);

            ObstacleVertexData oData;
            int gIndex = 0, index = 0, vCountMinusOne, firstIndex, lastIndex;

            for (int i = 0; i < obsCount; i++)
            {
                o = _obstacles[i];

                vCount = o.Count;
                vCountMinusOne = vCount - 1;
                firstIndex = gIndex;
                lastIndex = gIndex + vCountMinusOne;

                if (!o.edge)
                {
                    //Obstacle is a closed polygon
                    for (int v = 0; v < vCount; v++)
                    {
                        oData = new ObstacleVertexData()
                        {
                            infos = i,
                            index = index,
                            pos = new float2(o[v].pos.x, o[v].pos.y),
                            prev = v == 0 ? lastIndex : index - 1,
                            next = v == vCountMinusOne ? firstIndex : index + 1
                        };
                        _referenceObstacles[index++] = oData;
                    }
                }
                else
                {
                    //Obstacle is an open path
                    for (int v = 0; v < vCount; v++)
                    {
                        oData = new ObstacleVertexData()
                        {
                            infos = i,
                            index = index,
                            pos = new float2(o[v].pos.x, o[v].pos.y),
                            prev = v == 0 ? index : index - 1,
                            next = v == vCountMinusOne ? index : index + 1
                        };
                        _referenceObstacles[index++] = oData;
                    }

                }

                gIndex += vCount;
            }

            _referenceObstacles.CopyTo(_outputObstacles);
        }

        protected override void Apply(ref Unemployed job)
        {
            _recompute = false;
        }

        protected override void InternalDispose()
        {
            _obstacles = null;
            _outputObstacleInfos.Release();
            _referenceObstacles.Release();
            _outputObstacles.Release();
        }

    }

    public class StaticObstacleProvider : ObstacleProvider, IStaticObstacleProvider { }
    public class DynObstacleProvider : ObstacleProvider, IDynObstacleProvider
    {
        protected override void Prepare(ref Unemployed job, float delta)
        {
            _recompute = true; //force always recompute 
            base.Prepare(ref job, delta);
        }
    }


}
