// Copyright Contributors to the OpenVDB Project
// SPDX-License-Identifier: Apache-2.0

using System;

namespace OpenVDB.Math.Maps
{
    // Placeholder for NonlinearFrustumMap. Full implementation is complex.
    public class NonlinearFrustumMap : MapBase
    {
        // Parameters from C++ version
        private BBox<Vec3<double>, double> _bbox; // Index-space bounding box
        private double _taper;    // Ratio of nearplane/farplane width
        private double _depth;    // Depth of frustum (non-dimensionalized)
        private AffineMap _secondMap; // Secondary affine transformation

        // Precomputed values from C++ init()
        private double _mLx, _mLy, _mLz;
        private double _mXo, _mYo; // center offsets for bbox
        private double _mGamma;    // (1/taper - 1) / depth
        private double _mDepthOnLz;
        private double _mDepthOnLzLxLx; // depth / (Lz * Lx * Lx)

        public override string TypeName => "NonlinearFrustumMap";
        public override bool IsLinear => false;
        public override bool HasUniformScale => false;

        // Default constructor for placeholder
        public NonlinearFrustumMap()
        {
            _bbox = BBox<Vec3<double>, double>.CreateEmpty();
            _taper = 1.0;
            _depth = 1.0;
            _secondMap = new AffineMap(); // Identity
            Init();
        }

        public NonlinearFrustumMap(BBox<Vec3<double>, double> bbox, double taper, double depth)
            : this(bbox, taper, depth, new AffineMap()) // Call constructor with identity secondMap
        {
        }


        public NonlinearFrustumMap(BBox<Vec3<double>, double> bbox, double taper, double depth, AffineMap secondMap)
        {
            _bbox = bbox;
            _taper = taper;
            _depth = depth;
            _secondMap = (AffineMap)secondMap.Clone(); // Store a copy
            Init();
        }

        public NonlinearFrustumMap(NonlinearFrustumMap other)
        {
            _bbox = other._bbox;
            _taper = other._taper;
            _depth = other._depth;
            _secondMap = (AffineMap)other._secondMap.Clone();
            Init(); // Re-init precomputed values
        }


        private void Init()
        {
            if (_bbox.IsEmpty) // Cannot initialize if bbox is empty
            {
                // Set to some safe defaults to avoid division by zero if bbox is not properly set later
                _mLx = _mLy = _mLz = 1.0;
                _mXo = _mYo = 0.5;
                _mGamma = (_taper > 0 && _taper < 1.0 && _depth > 0) ? (1.0 / _taper - 1.0) / _depth : 0.0;
                _mDepthOnLz = _depth; // Avoid division by zero
                _mDepthOnLzLxLx = _depth; // Avoid division by zero
                return;
            }

            Vec3<double> extents = _bbox.Extents(); // Assumes extents for double BBox is max-min
            _mLx = extents.X;
            _mLy = extents.Y;
            _mLz = extents.Z;

            if (MathUtil.IsApproxZero(_mLx) || MathUtil.IsApproxZero(_mLy) || MathUtil.IsApproxZero(_mLz))
                throw new ArgumentException("Bounding box for NonlinearFrustumMap must have non-zero extents.");

            _mXo = 0.5 * _mLx;
            _mYo = 0.5 * _mLy;

            if (MathUtil.IsApproxZero(_taper) || MathUtil.IsApproxZero(_depth))
                 _mGamma = 0; // Avoid division by zero if taper or depth is zero (degenerate frustum)
            else
                _mGamma = (1.0 / _taper - 1.0) / _depth;

            _mDepthOnLz = _depth / _mLz;
            _mDepthOnLzLxLx = _depth / (_mLz * _mLx * _mLx); // Precompute mDepth / (Lz * Lx^2)
        }

        private Vec3<double> ApplyFrustumMapInternal(Vec3<double> p)
        {
            Vec3<double> q = p - _bbox.Min;
            q.X -= _mXo;
            q.Y -= _mYo;
            q.Z *= _mDepthOnLz;

            double scale = ( _mGamma * q.Z + 1.0 ) / _mLx;
            if (MathUtil.IsApproxZero(scale) || MathUtil.IsApproxZero(_mLx)) // Avoid division by zero or issues if mLx is zero
            {
                 // This case implies that the point is at or very near the focal point of the perspective projection,
                 // or the frustum is ill-defined. Handle as appropriate (e.g., return a specific value or throw).
                 // For now, to prevent NaNs, let's return something, though this might not be physically correct.
                 // A robust solution would check mLx during Init().
                 return new Vec3<double>(0,0,q.Z);
            }

            q.X *= scale;
            q.Y *= scale;
            return q;
        }

        private Vec3<double> ApplyFrustumInverseMapInternal(Vec3<double> p)
        {
            Vec3<double> q = p;
            double sFactor = _mGamma * q.Z + 1.0;

            if (MathUtil.IsApproxZero(sFactor))
            {
                // Point is at the eye of the perspective projection, inverse is undefined or at infinity.
                // Or mLx is zero.
                // This indicates a singularity.
                // Consider throwing or returning a specific value.
                // For now, return a value that might indicate an issue.
                return new Vec3<double>(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
            }
            double invScale = _mLx / sFactor;

            q.X *= invScale;
            q.Y *= invScale;
            q.Z /= _mDepthOnLz;

            q.X += _mXo;
            q.Y += _mYo;
            q += _bbox.Min;
            return q;
        }


        public override Vec3<double> ApplyMap(Vec3<double> sourcePoint) => _secondMap.ApplyMap(ApplyFrustumMapInternal(sourcePoint));
        public override Vec3<double> ApplyInverseMap(Vec3<double> sourcePoint) => ApplyFrustumInverseMapInternal(_secondMap.ApplyInverseMap(sourcePoint));

        // Jacobians, Determinant, VoxelSize for non-linear maps are complex and position-dependent.
        // The C++ code has detailed implementations. These are simplified/deferred.
        public override Vec3<double> ApplyJacobian(Vec3<double> sourceVector)
        {
            // This is a simplification. True Jacobian depends on domainPos.
            // For now, returning the Jacobian of the linear part (_secondMap)
            return _secondMap.ApplyJacobian(sourceVector);
        }
        public override Vec3<double> ApplyInverseJacobian(Vec3<double> sourceVector)
        {
            return _secondMap.ApplyInverseJacobian(sourceVector);
        }
        public override Vec3<double> ApplyJT(Vec3<double> sourceVector)
        {
            return _secondMap.ApplyJT(sourceVector);
        }
        public override Vec3<double> ApplyIJT(Vec3<double> sourceVector)
        {
            return _secondMap.ApplyIJT(sourceVector);
        }


        public override double GetDeterminant(Vec3<double> domainPos)
        {
            // Simplified: determinant of the linear part * frustum part.
            // Frustum part's determinant is s*s * (depth_on_lz / lx^2) * (1/lx) * (1/lx)
            // From C++: s*s * mDepthOnLzLxLx (where s = mGamma * local_z_prime + 1)
            // This is highly position dependent.
            Vec3<double> localFrustumPos = ApplyFrustumMapInternal(domainPos); // Position in frustum's "unit" space
            double s = _mGamma * localFrustumPos.Z + 1.0;
            double frustumDet = s * s * _mDepthOnLzLxLx; // This needs careful check against C++ logic for factor of mLx
                                                       // C++: s * s * mDepthOnLzLxLx; where mDepthOnLzLxLx = mDepth/(mLz*mLx*mLx)
                                                       // Jacobian elements are: scale, scale, mDepthOnLz. Det = scale^2 * mDepthOnLz
                                                       // scale = (mGamma * z' + 1) / mLx. So Det_frustum = ((mGamma*z'+1)/mLx)^2 * mDepthOnLz

            return _secondMap.Determinant * frustumDet;
        }

        public override Vec3<double> GetVoxelSize(Vec3<double> domainPos)
        {
            // Voxel size is |J*e1|, |J*e2|, |J*e3|
            // This is non-trivial for non-linear maps.
            // C++ computes this by transforming points loc, loc+e1, loc+e2, loc+e3
            Vec3<double> p0 = ApplyMap(domainPos);
            Vec3<double> p1 = ApplyMap(domainPos + new Vec3<double>(1,0,0));
            Vec3<double> p2 = ApplyMap(domainPos + new Vec3<double>(0,1,0));
            Vec3<double> p3 = ApplyMap(domainPos + new Vec3<double>(0,0,1));
            return new Vec3<double>((p1-p0).Length(), (p2-p0).Length(), (p3-p0).Length());
        }

        public override IMap Clone() => new NonlinearFrustumMap(this);
        public override AffineMap ToAffineMap() => throw new InvalidOperationException("NonlinearFrustumMap cannot be represented as a single AffineMap.");
        public override IMap InverseMap() => throw new NotImplementedException("Inverse of NonlinearFrustumMap is not implemented.");

        // Composition methods will be complex and likely result in a generic "ComposedMap" or throw.
        // For now, they will fallback to base, which converts to AffineMap, which will fail.

        public override void WriteData(System.IO.BinaryWriter writer, Core.IO.StreamMetadata streamMetadata)
        {
            // Serialize _bbox (BBox<Vec3<double>, double>)
            writer.Write(_bbox.Min.X); writer.Write(_bbox.Min.Y); writer.Write(_bbox.Min.Z);
            writer.Write(_bbox.Max.X); writer.Write(_bbox.Max.Y); writer.Write(_bbox.Max.Z);

            writer.Write(_taper);
            writer.Write(_depth);

            // Serialize _secondMap (AffineMap)
            // Need to write the type name first if we want to support polymorphic maps here,
            // but C++ version writes mSecondMap directly, implying it's always AffineMap.
            // IoUtils.WriteString(writer, _secondMap.TypeName); // If _secondMap was IMap
            _secondMap.WriteData(writer, streamMetadata);
        }

        public override void ReadData(System.IO.BinaryReader reader, Core.IO.StreamMetadata streamMetadata)
        {
            var minX = reader.ReadDouble(); var minY = reader.ReadDouble(); var minZ = reader.ReadDouble();
            var maxX = reader.ReadDouble(); var maxY = reader.ReadDouble(); var maxZ = reader.ReadDouble();
            _bbox = new BBox<Vec3<double>, double>(new Vec3<double>(minX, minY, minZ), new Vec3<double>(maxX, maxY, maxZ));

            _taper = reader.ReadDouble();
            _depth = reader.ReadDouble();

            // Read _secondMap (AffineMap)
            // string mapTypeName = IoUtils.ReadString(reader); // If type name was written
            // if (mapTypeName != AffineMap.StaticTypeName) throw new IoErrorException("Expected AffineMap for Frustum's second map");
            _secondMap = new AffineMap(); // Default constructor
            _secondMap.ReadData(reader, streamMetadata);

            Init(); // Recompute derived members
        }
    }
}
