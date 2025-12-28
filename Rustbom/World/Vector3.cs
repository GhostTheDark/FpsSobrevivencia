using System;

namespace RustlikeServer.World
{
    /// <summary>
    /// Vector3 simples para posições 3D
    /// </summary>
    public struct Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 Zero => new Vector3(0, 0, 0);

        public float Distance(Vector3 other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            float dz = Z - other.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public override string ToString()
        {
            return $"({X:F2}, {Y:F2}, {Z:F2})";
        }
    }

    /// <summary>
    /// Vector2 simples para rotações (Yaw, Pitch)
    /// </summary>
    public struct Vector2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 Zero => new Vector2(0, 0);

        public override string ToString()
        {
            return $"({X:F2}, {Y:F2})";
        }
    }
}