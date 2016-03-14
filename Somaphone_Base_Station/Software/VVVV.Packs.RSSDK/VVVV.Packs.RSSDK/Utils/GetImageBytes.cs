using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libpxcclr;
using libpxcclr.cs;

namespace VVVV.Nodes.RSSDK
{
    static class GetImageBytes
    {
    	
        public static byte[] GetRGB32Pixels(this PXCMImage image, out int cwidth, out int cheight)
        {
            PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            cwidth = cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cwidth = (int)cdata.pitches[0] / sizeof(Int32);
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, (int)cdata.pitches[0] * cheight);
                image.ReleaseAccess(cdata);
            }
            return cpixels;
        }
        public static byte[] GetY8Pixels(this PXCMImage image, out int cwidth, out int cheight)
        {
            PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            cwidth = cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_Y8, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cwidth = (int)cdata.pitches[0] / sizeof(byte);
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, (int)cdata.pitches[0] * cheight);
                image.ReleaseAccess(cdata);
            }
            return cpixels;
        }
        public static byte[] GetY8IrRelativePixels(this PXCMImage image, out int cwidth, out int cheight)
        {
            PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            cwidth = cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_Y8_IR_RELATIVE, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cwidth = (int)cdata.pitches[0] / sizeof(byte);
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, (int)cdata.pitches[0] * cheight);
                image.ReleaseAccess(cdata);
            }
            return cpixels;
        }
        public static byte[] GetY16Pixels(this PXCMImage image, out int cwidth, out int cheight)
        {
            PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            cwidth = cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_Y16, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cwidth = (int)cdata.pitches[0] / sizeof(ushort);
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, (int)cdata.pitches[0] * cheight);
                image.ReleaseAccess(cdata);
            }
            return cpixels;
        }
        public static byte[] GetDepthPixels(this PXCMImage image, out int cwidth, out int cheight)
        {
            PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            cwidth = cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cwidth = (int)cdata.pitches[0] / sizeof(ushort);
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, (int)cdata.pitches[0] * cheight);
                image.ReleaseAccess(cdata);
            }
            return cpixels;
        }
        public static byte[] GetDepthF32Pixels(this PXCMImage image, out int cwidth, out int cheight)
        {
            PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            cwidth = cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH_F32, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cwidth = (int)cdata.pitches[0] / sizeof(float);
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, (int)cdata.pitches[0] * cheight);
                image.ReleaseAccess(cdata);
            }
            return cpixels;
        }
        public static byte[] GetDepthRawPixels(this PXCMImage image, out int cwidth, out int cheight)
        {
            PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            cwidth = cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH_RAW, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cwidth = (int)cdata.pitches[0] / sizeof(ushort);
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, (int)cdata.pitches[0] * cheight);
                image.ReleaseAccess(cdata);
            }
            return cpixels;
        }

        public static byte[] GetRGB32Pixels(this PXCMImage image)
        {
            PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            int cwidth, cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cwidth = (int)cdata.pitches[0] / sizeof(Int32);
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, (int)cdata.pitches[0] * cheight);
                image.ReleaseAccess(cdata);
            }
            return cpixels;
        }
        public static byte[] GetY8Pixels(this PXCMImage image)
        {
            PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            int cwidth, cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_Y8, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cwidth = (int)cdata.pitches[0] / sizeof(byte);
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, (int)cdata.pitches[0] * cheight);
                image.ReleaseAccess(cdata);
            }
            return cpixels;
        }
        public static byte[] GetY8IrRelativePixels(this PXCMImage image)
        {
            PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            int cwidth, cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_Y8_IR_RELATIVE, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cwidth = (int)cdata.pitches[0] / sizeof(byte);
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, (int)cdata.pitches[0] * cheight);
                image.ReleaseAccess(cdata);
            }
            return cpixels;
        }
        
        public static byte[] GetY16Pixels(this PXCMImage image)
        {
            PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            int cwidth, cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_Y16, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cwidth = (int)cdata.pitches[0] / sizeof(ushort);
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, (int)cdata.pitches[0] * cheight);
                image.ReleaseAccess(cdata);
            }
            return cpixels;
        }
        
        public static byte[] GetYUY2Pixels(this PXCMImage image)
        {
        	PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            int cwidth, cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_YUY2, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
            	cwidth = ((int)cdata.pitches[0] / sizeof(ushort)) + ((int)cdata.pitches[1] / sizeof(ushort));
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, ((int)cdata.pitches[0]+(int)cdata.pitches[1]) * cheight);
                image.ReleaseAccess(cdata);
            }
            return cpixels;
        }
        	
        public static byte[] GetDepthPixels(this PXCMImage image)
        {
            PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            int cwidth, cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cwidth = (int)cdata.pitches[0] / sizeof(Int32); //ushort
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, (int)cdata.pitches[0] * cheight);
                image.ReleaseAccess(cdata);
                image.info.format = PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH; //PIXEL_FORMAT_DEPTH;
            }
            return cpixels;
        }
        public static byte[] GetDepthF32Pixels(this PXCMImage image)
        {
            PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            int cwidth, cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH_F32, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cwidth = (int)cdata.pitches[0] / sizeof(float);
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, (int)cdata.pitches[0] * cheight);
                image.ReleaseAccess(cdata);
                image.info.format = PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH_F32;
            }
            
            return cpixels;
        }
        public static byte[] GetDepthRawPixels(this PXCMImage image)
        {
            PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            int cwidth, cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH_RAW, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cwidth = (int)cdata.pitches[0] / sizeof(ushort);
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, (int)cdata.pitches[0] * cheight);
                image.ReleaseAccess(cdata);
                image.info.format = PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH_RAW;
            }
            return cpixels;
        }
    }
}
