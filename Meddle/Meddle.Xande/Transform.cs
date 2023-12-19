using FFXIVClientStructs.Havok;
using SharpGLTF.Transforms;
using System.Numerics;
using System.Text.Json.Serialization;
using CSTransform = FFXIVClientStructs.FFXIV.Client.Graphics.Transform;

namespace Meddle.Xande;

public readonly record struct Transform
{
    [JsonIgnore]
    public Vector3 Translation { get; init; }
    [JsonIgnore]
    public Quaternion Rotation { get; init; }
    [JsonIgnore]
    public Vector3 Scale { get; init; }

    [JsonIgnore]
    public AffineTransform AffineTransform => new(Scale, Rotation, Translation);

    public Transform(hkQsTransformf hkTransform)
    {
        Translation = AsVector(hkTransform.Translation);
        Rotation = AsQuaternion(hkTransform.Rotation);
        Scale = AsVector(hkTransform.Scale);
    }

    public Transform(CSTransform transform)
    {
        Translation = transform.Position;
        Rotation = transform.Rotation;
        Scale = transform.Scale;
    }

    public Transform(AffineTransform transform)
    {
        transform = transform.GetDecomposed();
        Translation = transform.Translation;
        Rotation = transform.Rotation;
        Scale = transform.Scale;
    }

    public Transform(Matrix4x4 transform) : this(new AffineTransform(transform))
    {

    }

    public override string ToString()
    {
        return $"{Translation:0.00} {new EulerAngles(Rotation).Angles:0.00} {Scale:0.00}";
    }

    private static Vector3 AsVector(hkVector4f hkVector) =>
        new(hkVector.X, hkVector.Y, hkVector.Z);

    private static Vector4 AsVector(hkQuaternionf hkVector) =>
        new(hkVector.X, hkVector.Y, hkVector.Z, hkVector.W);

    private static Quaternion AsQuaternion(hkQuaternionf hkQuaternion) =>
        new(hkQuaternion.X, hkQuaternion.Y, hkQuaternion.Z, hkQuaternion.W);
}

public readonly record struct EulerAngles
{
    public Vector3 Angles { get; init; }

    // https://stackoverflow.com/a/70462919
    public EulerAngles(Quaternion q)
    {
        Vector3 angles = new();

        // roll / x
        double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

        // pitch / y
        double sinp = 2 * (q.W * q.Y - q.Z * q.X);
        if (Math.Abs(sinp) >= 1)
        {
            angles.Y = (float)Math.CopySign(Math.PI / 2, sinp);
        }
        else
        {
            angles.Y = (float)Math.Asin(sinp);
        }

        // yaw / z
        double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

        Angles = angles * 180 / MathF.PI;
    }

    public static implicit operator Vector3(EulerAngles e) => e.Angles;

    public override string ToString()
    {
        return Angles.ToString();
    }

    public readonly string ToString(string? format)
    {
        return Angles.ToString(format);
    }

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return Angles.ToString(format, formatProvider);
    }
}
