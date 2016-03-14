using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.Utils.VMath;
using libpxcclr;
using libpxcclr.cs;

namespace VVVV.Nodes.RSSDK
{
    static class VectorConverter
    {
    	public static Vector2D Vector2DtoVVVV(this Vector2D pt, Vector2D ImageSize)
    	{
    		pt.x = pt.x / (float)ImageSize.x * 2.0f - 1.0f;
			pt.y = 1.0f - pt.y / (float)ImageSize.y * 2.0f;
			return pt;
    	}
    	
    	public static Vector3D Vector3DtoVVVV(this Vector3D pt, Vector2D ImageSize)
    	{
    		pt.x = pt.x / (float)ImageSize.x * 2.0f - 1.0f;
			pt.y = 1.0f - pt.y / (float)ImageSize.y * 2.0f;
			//pt.z
			return pt;
    	}
    	
    	public static Vector4D BBox2DtoVVVV(this PXCMRectI32 pt, Vector2D ImageSize)
    	{
    		var bbox2d = new Vector4D();
    		
    		bbox2d.z = pt.w / (float)ImageSize.x;
			bbox2d.w = pt.h / (float)ImageSize.y;
    		
			bbox2d.x = (pt.x / (float)ImageSize.x * 2.0f - 1.0f) + bbox2d.z;
    		bbox2d.y = (1.0f - pt.y / (float)ImageSize.y * 2.0f) - bbox2d.w;
			
			bbox2d.z *= 2;
			bbox2d.w *= 2;
			
			return bbox2d;
    	}
    	
        #region Vector2D
        public static Vector2D ToVector2D(this PXCMPointF32 point)
        {
            return new Vector2D(point.x, point.y);
        }
        public static Vector2D ToVector2D(this PXCMPointI32 point)
        {
            return new Vector2D(point.x, point.y);
        }
        public static Vector2D ToVector2D(this PXCMRangeF32 point)
        {
            return new Vector2D(point.min, point.max);
        }
        public static Vector2D ToVector2D(this PXCMSizeI32 point)
        {
            return new Vector2D(point.width, point.height);
        }

        public static PXCMPointF32 ToPXCMPointF32(this Vector2D v)
        {
            var ret = new PXCMPointF32();
            ret.x = (float)v.x;
            ret.y = (float)v.y;
            return ret;
        }
        public static PXCMPointI32 ToPXCMPointI32(this Vector2D v)
        {
            var ret = new PXCMPointI32();
            ret.x = (int)v.x;
            ret.y = (int)v.y;
            return ret;
        }
        public static PXCMRangeF32 ToPXCMRangeF32(this Vector2D v)
        {
            var ret = new PXCMRangeF32();
            ret.min = (float)v.x;
            ret.max = (float)v.y;
            return ret;
        }
        public static PXCMSizeI32 ToPXCMSizeI32(this Vector2D v)
        {
            var ret = new PXCMSizeI32();
            ret.width = (int)v.x;
            ret.height = (int)v.y;
            return ret;
        }
        #endregion Vector2D

        #region Vector3D
        public static Vector3D ToVector3D(this PXCMPoint3DF32 point)
        {
            return new Vector3D(point.x, point.y, point.z);
        }

        public static PXCMPoint3DF32 ToPXCMPoint3DF32(this Vector3D v)
        {
            var ret = new PXCMPoint3DF32();
            ret.x = (float)v.x;
            ret.y = (float)v.y;
            ret.z = (float)v.z;
            return ret;
        }
        #endregion Vector3D

        #region Vector4D
        public static Vector4D ToVector4D(this PXCMPoint4DF32 point)
        {
            return new Vector4D(point.x, point.y, point.z, point.w);
        }
        public static Vector4D ToVector4D(this PXCMRectI32 point)
        {
            return new Vector4D(point.x, point.y, point.w, point.h);
        }
        public static Vector4D ToVector4D(this PXCMFaceData.PoseQuaternion point)
        {
            return new Vector4D(point.x, point.y, point.z, point.w);
        }
        public static Vector3D ToYawPitchRoll(this PXCMFaceData.PoseQuaternion point)
        {
            Vector3D orig = VMath.QuaternionToEulerYawPitchRoll(point.ToVector4D());
            return new Vector3D(orig.z, orig.y, orig.x);
        }

        public static PXCMPoint4DF32 ToPXCMPoint4DF32(this Vector4D v)
        {
            var ret = new PXCMPoint4DF32();
            ret.x = (float)v.x;
            ret.y = (float)v.y;
            ret.z = (float)v.z;
            ret.w = (float)v.w;
            return ret;
        }
        public static PXCMRectI32 ToPXCMRectI32(this Vector4D v)
        {
            var ret = new PXCMRectI32();
            ret.x = (int)v.x;
            ret.y = (int)v.y;
            ret.w = (int)v.z;
            ret.h = (int)v.w;
            return ret;
        }
        #endregion Vector4D
    }
}
