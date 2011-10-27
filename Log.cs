using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.DirectX;

namespace BuildBrickBuilding
{
    public static class Log
    {
        public static void show(float f)
        {
            Console.WriteLine(f.ToString());
        }

        public static void show(string s)
        {
            Console.WriteLine(s);
        }

        public static void show(Vector3 v3)
        {
            Console.WriteLine("({0},{1},{2}) {3}", v3.X, v3.Y, v3.Z, v3.Length());
        }

        public static void show(Vector3 v3, string s)
        {
            Console.Write(s);
            Console.WriteLine(" ({0},{1},{2}) {3}", v3.X, v3.Y, v3.Z, v3.Length());
        }

        public static void show(Block block)
        {
            if (block != null)
            {
                show(block.getPos(), block.getColor().ToString());
            }
        }

        public static void show(Brick brick)
        {
            foreach (Block block in brick.getBlocks())
            {
                show(block);
            }
        }
    }
}
