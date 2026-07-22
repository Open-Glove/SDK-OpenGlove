using System;

namespace OpenGlovePrototype2
{
    /// <summary>
    /// Matemáticas para modelo segmentario: cuaterniones unitarios y vectores 3D.
    /// Inverso de cuaternión unitario = conjugado. Rotación: v' = q * v * q^-1.
    /// </summary>
    public static class ArmTrackingMath
    {
        public struct Quat
        {
            public float W, X, Y, Z;
            public Quat(float w, float x, float y, float z) { W = w; X = x; Y = y; Z = z; }
        }

        public struct Vec3
        {
            public float X, Y, Z;
            public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }
        }

        /// <summary>Conjugado = inverso para cuaternión unitario: (w, -x, -y, -z).</summary>
        public static Quat Conjugate(Quat q)
        {
            return new Quat(q.W, -q.X, -q.Y, -q.Z);
        }

        /// <summary>Multiplicación de cuaterniones: q1 * q2.</summary>
        public static Quat Multiply(Quat a, Quat b)
        {
            return new Quat(
                a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z,
                a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
                a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W
            );
        }

        /// <summary>Rota el vector v por el cuaternión unitario q: v' = q * (0,v) * q^-1. Devuelve el vector resultante.</summary>
        public static Vec3 Rotate(Quat q, Vec3 v)
        {
            // p = (0, v.X, v.Y, v.Z); resultado = q * p * Conjugate(q)
            Quat p = new Quat(0, v.X, v.Y, v.Z);
            Quat qInv = Conjugate(q);
            Quat result = Multiply(Multiply(q, p), qInv);
            return new Vec3(result.X, result.Y, result.Z);
        }

        public static Vec3 Add(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vec3 Subtract(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }
    }
}
