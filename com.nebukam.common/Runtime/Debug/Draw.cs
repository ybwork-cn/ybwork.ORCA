using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Nebukam.Common
{
    static public class Draw
    {
        /// <summary>
        /// Draw a line between two positions.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Line2D(float2 from, float2 to)
        {
            Line2D(from, to, Color.red);
        }

        /// <summary>
        /// Draw a line between two positions, with a specific color.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="col"></param>
        public static void Line2D(float2 from, float2 to, Color col)
        {
            Debug.DrawLine(new float3(from.x, 0, from.y), new float3(to.x, 0, to.y), col, 0);
        }

        /// <summary>
        /// Draw a line between two positions.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void Line(float3 from, float3 to)
        {
            Line(from, to, Color.red);
        }

        /// <summary>
        /// Draw a line between two positions, with a specific color.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="col"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void Line(float3 from, float3 to, Color col)
        {
            Debug.DrawLine(from, to, col, 0, depthTest: false);
        }

        /// <summary>
        /// Draw a circle (XZ plane).
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void Circle(float3 center, float radius, int samples = 30)
        {
            Circle(center, radius, Color.red, samples);
        }

        /// <summary>
        /// Draw a circle (XZ plane), with a specific color.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="col"></param>
        static public void Circle(float3 center, float radius, Color col, int samples = 30)
        {
            float angleIncrease = (float)(math.PI * 2) / samples;
            float3 from = new float3(center.x + radius * (float)math.cos(0.0f), center.y, center.z + radius * (float)Math.Sin(0.0f));

            for (int i = 0; i < samples; i++)
            {

                float rad = angleIncrease * (i + 1);

                float3 to = new float3(center.x + radius * math.cos(rad), center.y, center.z + radius * math.sin(rad));

                Line(from, to, col);

                from = to;
            }
        }

        /// <summary>
        /// Draw a circle (XY plane).
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void Circle2D(float2 center, float radius, int samples = 30)
        {
            Circle2D(center, radius, Color.red, samples);
        }

        /// <summary>
        /// Draw a circle (XY plane), with a specific color.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="col"></param>
        static public void Circle2D(float2 center, float radius, Color col, int samples = 30)
        {
            float angleIncrease = (float)(math.PI * 2) / samples;
            float2 from = new float2(center.x + radius * math.cos(0.0f), center.y + radius * math.sin(0.0f));

            for (int i = 0; i < samples; i++)
            {
                float rad = angleIncrease * (i + 1);

                float2 to = new float2(center.x + radius * math.cos(rad), center.y + radius * math.sin(rad));

                Line2D(from, to, col);

                from = to;
            }
        }

        /// <summary>
        /// Draw a square at the specified location (XZ plane).
        /// </summary>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <param name="col"></param>
        static public void Square(float3 center, float size, Color col)
        {
            float s = size * 0.5f;

            float3 A = center + new float3(-s, 0f, -s);
            float3 B = center + new float3(-s, 0f, s);
            float3 C = center + new float3(s, 0f, s);
            float3 D = center + new float3(s, 0f, -s);

            Line(A, B, col);
            Line(B, C, col);
            Line(C, D, col);
            Line(D, A, col);
        }

        /// <summary>
        /// Draw a square at the specified location (XZ plane).
        /// </summary>
        /// <param name="center"></param>
        /// <param name="size"></param>
        /// <param name="col"></param>
        static public void Cube(float3 center, float size, Color col)
        {
            float s = size * 0.5f;

            float3 A = center + new float3(-s, s, -s);
            float3 B = center + new float3(-s, s, s);
            float3 C = center + new float3(s, s, s);
            float3 D = center + new float3(s, s, -s);

            float3 E = center + new float3(-s, -s, -s);
            float3 F = center + new float3(-s, -s, s);
            float3 G = center + new float3(s, -s, s);
            float3 H = center + new float3(s, -s, -s);

            Line(A, B, col);
            Line(B, C, col);
            Line(C, D, col);
            Line(D, A, col);

            Line(E, F, col);
            Line(F, G, col);
            Line(G, H, col);
            Line(H, E, col);

            Line(A, E, col);
            Line(B, F, col);
            Line(C, G, col);
            Line(D, H, col);
        }
    }
}
