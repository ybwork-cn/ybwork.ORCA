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

namespace Nebukam.ORCA
{

    /*
     Bitwise with flags :

     AB = A | B
     AB & ~B = A 
     ABC = A | B | C //Combine a set of flags
     ABC & ~AB = C //Remove a set of flags
     ( foo & AB) == AB ? //foo has AB set.
     ( foo & AB ) != 0 ? //foo has A and/or B set.
     foo ^= A; // Toggle B in foo
     foo &= ~A; //Remove A from foo
     foo |= A; //Adds A to foo
     */

    /// <summary>
    /// Available layers to manage Agents, Obstacles & Raycasts
    /// </summary>
    [System.Flags]
    public enum ORCALayer : uint
    {
        NONE = 0x00,
        L0 = 0x01,
        L1 = 0x02,
        L2 = 0x04,
        L3 = 0x08,
        L4 = 0x10,
        L5 = 0x20,
        L6 = 0x40,
        L7 = 0x80,
        L8 = 0x100,
        L9 = 0x200,
        L10 = 0x400,
        L11 = 0x800,
        L12 = 0x1000,
        L13 = 0x2000,
        L14 = 0x4000,
        L15 = 0x8000,
        L16 = 0x10000,
        L17 = 0x20000,
        L18 = 0x40000,
        L19 = 0x80000,
        L20 = 0x100000,
        L21 = 0x200000,
        L22 = 0x400000,
        L23 = 0x800000,
        L24 = 0x1000000,
        L25 = 0x2000000,
        L26 = 0x4000000,
        L27 = 0x8000000,
        L28 = 0x10000000,
        L29 = 0x20000000,
        L30 = 0x40000000,
        L31 = 0x80000000,
        ANY =
        L0 | L1 | L2 | L3 |
        L4 | L5 | L6 | L7 |
        L8 | L9 | L10 | L11 |
        L12 | L13 | L14 | L15 |
        L16 | L17 | L18 | L19 |
        L20 | L21 | L22 | L23 |
        L24 | L25 | L26 | L27 |
        L28 | L29 | L30 | L31
    }
}
