using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.DirectX;
using Microsoft.Research.Kinect.Nui;

namespace BuildBrickBuilding
{
    class Nui
    {

        public const float CLOSE_DIS2 = 0.03f;

        public static Vector3 toVector3(Joint joint)
        {
            return new Vector3(joint.Position.X, joint.Position.Y, joint.Position.Z);
        }

        public static float dist(Vector3 a, Vector3 b)
        {
            return (a - b).Length();
        }

        public static float dist2(Vector3 a, Vector3 b)
        {
            return (a - b).LengthSq();
        }

        public static bool isNear(Vector3 a, Vector3 b)
        {
            if ((a - b).LengthSq() <= CLOSE_DIS2)
            {
                return true;
            }
            return false;
        }
    }
}
