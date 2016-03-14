using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V1;
using VVVV.Hosting;
using VVVV.Utils.VMath;
using VVVV.Core.Collections;
using libpxcclr;
using libpxcclr.cs;

namespace VVVV.Nodes.RSSDK
{
    public class RSSDKLandmark
    {
        public int Index = -1;
        public PXCMFaceData.LandmarkType Type = PXCMFaceData.LandmarkType.LANDMARK_NOT_NAMED;
        public int ImageConfidence = -1;
        public Vector2D ImagePosition;
        public int WorldConfidence = -1;
        public Vector3D WorldPosition;

        public RSSDKLandmark() { }
    }
    public class RSSDKExpression
    {
        public PXCMFaceData.ExpressionsData.FaceExpression Type;
        public int Intensity = -1;
        public bool Detected = false;

        public RSSDKExpression() { }
    }
    public class RSSDKFace
    {
        public int UserID = -1;
        public Vector4D Bounds;
        public float AverageDepth = -1;
        public Vector4D Quaternion;
        public Vector3D YawPitchRoll;
        public Vector3D WorldPosition;

        public Spread<RSSDKLandmark> Landmarks = new Spread<RSSDKLandmark>();
        public Spread<RSSDKExpression> Expressions = new Spread<RSSDKExpression>();

        public Stopwatch Expiry = new Stopwatch();

        public RSSDKFace()
        {
            this.Expiry.Start();
        }
    }
    public class RSSDKFaceConfig
    {
        public PXCMFaceData FaceData;
        public PXCMFaceConfiguration FaceConfig;
        public PXCMFaceConfiguration.ExpressionsConfiguration ExpressionConfig;
        public PXCMFaceConfiguration.RecognitionConfiguration RecognitionConfig;
        public RSSDKFaceConfig() { }
    }
    [PluginInfo(Name = "Face", Category = "RSSDK")]
    public class RSSDKFaceTrackingNode : IPluginEvaluate
    {
        [Input("Context")]
        public ISpread<RSSDKContext> FSenseWrap;

        [Output("Faces")]
        public ISpread<RSSDKFace> FFace;
        [Output("ID")]
        public ISpread<int> FID;

        [Output("Face Bounds")]
        public ISpread<Vector4D> FFaceBounds;
        [Output("Face Average Depth")]
        public ISpread<float> FAverageDepth;

        [Output("Rotation Quaternion")]
        public ISpread<Vector4D> FRotQuat;
        [Output("Rotation Euler")]
        public ISpread<Vector3D> FRot;
        [Output("Position")]
        public ISpread<Vector3D> FPos;

        void IPluginEvaluate.Evaluate(int SpreadMax)
        {
            if((FSenseWrap[0] != null))
            {
                FFace.SliceCount = FSenseWrap[0].Faces.Count;
                FFaceBounds.SliceCount = FSenseWrap[0].Faces.Count;
                FAverageDepth.SliceCount = FSenseWrap[0].Faces.Count;
                FRotQuat.SliceCount = FSenseWrap[0].Faces.Count;
                FRot.SliceCount = FSenseWrap[0].Faces.Count;
                FPos.SliceCount = FSenseWrap[0].Faces.Count;
                int i=0;
                foreach(KeyValuePair<int, RSSDKFace> kvp in FSenseWrap[0].Faces)
                {
                    FFace[i] = kvp.Value;
                    FID[i] = kvp.Value.UserID;
                    FFaceBounds[i] = kvp.Value.Bounds;
                    FAverageDepth[i] = kvp.Value.AverageDepth;
                    FRotQuat[i] = kvp.Value.Quaternion;
                    FRot[i] = kvp.Value.YawPitchRoll;
                    FPos[i] = kvp.Value.WorldPosition;
                    i++;
                }
            }
        }
    }

    [PluginInfo(Name = "Face", Category = "RSSDK", Version = "Split")]
    public class RSSDKFaceSplitNode : IPluginEvaluate
    {
        [Input("Input")]
        public Pin<RSSDKFace> FFace;

        [Output("ID")]
        public ISpread<int> FID;
        [Output("Face Bounds")]
        public ISpread<Vector4D> FFaceBounds;
        [Output("Face Average Depth")]
        public ISpread<float> FAverageDepth;
        [Output("Rotation Quaternion")]
        public ISpread<Vector4D> FRotQuat;
        [Output("Rotation Euler")]
        public ISpread<Vector3D> FRot;
        [Output("Position")]
        public ISpread<Vector3D> FPos;

        [Output("Landmarks")]
        public ISpread<ISpread<RSSDKLandmark>> FLandmarks;
        [Output("Expressions")]
        public ISpread<ISpread<RSSDKExpression>> FExpressions;

        void IPluginEvaluate.Evaluate(int SpreadMax)
        {
            if (FFace.IsConnected)
            {
                FFaceBounds.SliceCount = FFace.SliceCount;
                FAverageDepth.SliceCount = FFace.SliceCount;
                FRotQuat.SliceCount = FFace.SliceCount;
                FRot.SliceCount = FFace.SliceCount;
                FPos.SliceCount = FFace.SliceCount;
                FLandmarks.SliceCount = FFace.SliceCount;
                FExpressions.SliceCount = FFace.SliceCount;
                for(int i=0; i<FFace.SliceCount; i++)
                {
                    if (FFace[i] != null)
                    {
                        FID[i] = FFace[i].UserID;
                        FFaceBounds[i] = FFace[i].Bounds;
                        FAverageDepth[i] = FFace[i].AverageDepth;
                        FRotQuat[i] = FFace[i].Quaternion;
                        FRot[i] = FFace[i].YawPitchRoll;
                        FPos[i] = FFace[i].WorldPosition;
                        FLandmarks[i].SliceCount = FFace[i].Landmarks.SliceCount;
                        FExpressions[i].SliceCount = FFace[i].Expressions.SliceCount;
                        for (int j = 0; j < FLandmarks[i].SliceCount; j++)
                            FLandmarks[i][j] = FFace[i].Landmarks[j];
                        for (int j = 0; j < FExpressions[i].SliceCount; j++)
                            FExpressions[i][j] = FFace[i].Expressions[j];
                    }
                }
            }
        }
    }

    [PluginInfo(Name = "Landmark", Category = "RSSDK", Version = "Split")]
    public class RSSDKLandmarkSplitNode : IPluginEvaluate
    {
        [Input("Input")]
        public Pin<RSSDKLandmark> FLandmark;

        [Output("ID")]
        public ISpread<int> FID;
        [Output("Type")]
        public ISpread<PXCMFaceData.LandmarkType> FType;
        [Output("Image Space Confidence")]
        public ISpread<double> FImgConf;
        [Output("Image Position")]
        public ISpread<Vector2D> FImgPos;
        [Output("World Space Confidence")]
        public ISpread<double> FWorldConf;
        [Output("World Position")]
        public ISpread<Vector3D> FWorldPos;


        void IPluginEvaluate.Evaluate(int SpreadMax)
        {
            if (FLandmark.IsConnected)
            {
                FID.SliceCount = FLandmark.SliceCount;
                FType.SliceCount = FLandmark.SliceCount;
                FImgConf.SliceCount = FLandmark.SliceCount;
                FImgPos.SliceCount = FLandmark.SliceCount;
                FWorldConf.SliceCount = FLandmark.SliceCount;
                FWorldPos.SliceCount = FLandmark.SliceCount;
                for (int i = 0; i < FLandmark.SliceCount; i++)
                {
                    if (FLandmark[i] != null)
                    {
                        FID[i] = FLandmark[i].Index;
                        FType[i] = FLandmark[i].Type;
                        FImgConf[i] = ((double)FLandmark[i].ImageConfidence) / 100;
                        FImgPos[i] = FLandmark[i].ImagePosition;
                        FWorldConf[i] = ((double)FLandmark[i].WorldConfidence) / 100;
                        FWorldPos[i] = FLandmark[i].WorldPosition;
                    }
                }
            }
        }
    }
    [PluginInfo(Name = "Expression", Category = "RSSDK", Version = "Split")]
    public class RSSDKExpressionSplitNode : IPluginEvaluate
    {
        [Input("Input")]
        public Pin<RSSDKExpression> FExpression;

        [Output("Type")]
        public ISpread<PXCMFaceData.ExpressionsData.FaceExpression> FType;
        [Output("Detected")]
        public ISpread<bool> FDetected;
        [Output("Intensity")]
        public ISpread<double> FIntensity;


        void IPluginEvaluate.Evaluate(int SpreadMax)
        {
            if (FExpression.IsConnected)
            {
                FType.SliceCount = FExpression.SliceCount;
                FDetected.SliceCount = FExpression.SliceCount;
                FIntensity.SliceCount = FExpression.SliceCount;
                for (int i = 0; i < FExpression.SliceCount; i++)
                {
                    if (FExpression[i] != null)
                    {
                        FType[i] = FExpression[i].Type;
                        FDetected[i] = FExpression[i].Detected;
                        FIntensity[i] = ((double)FExpression[i].Intensity) / 100;
                    }
                }
            }
        }
    }
}
