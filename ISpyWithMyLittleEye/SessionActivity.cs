using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Hardware.Camera2;
using Android.Util;
using Android.Hardware.Camera2.Params;
using Android.Graphics;
using Android.Media;
using Java.IO;
using Java.Nio;

namespace ISpyWithMyLittleEye
{
    [Activity(Label = "SessionActivity")]
    public class SessionActivity : Activity
    {
        CameraDevice cameraDevice;
        CameraStateListener stateListener;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.SessionActivityLayout);
            // Create your application here
            var spyButton = FindViewById<Button>(Resource.Id.spyButton);
            spyButton.Click += (sender, args) =>
            {
                TakePicture();
            };
            
        }

        protected override void OnResume()
        {
            base.OnResume();
            OpenCamera();
        }

        protected override void OnPause()
        {
            base.OnPause();
            if (cameraDevice != null)
            {
                cameraDevice.Close();
                cameraDevice = null;
            }
        }

        protected void OpenCamera()
        {
            CameraManager manager = ((CameraManager)GetSystemService(CameraService));
            string cameraId = manager.GetCameraIdList()[0];
            stateListener = new CameraStateListener(this);
            manager.OpenCamera(cameraId, stateListener, null);
        }

        protected void TakePicture()
        {
            CameraManager manager = ((CameraManager)GetSystemService(CameraService));
            CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraDevice.Id);
            Size[] jpegSizes = ((StreamConfigurationMap)characteristics
                .Get(CameraCharacteristics.ScalerStreamConfigurationMap)).GetOutputSizes((int)ImageFormatType.Jpeg);
            int width = jpegSizes[0].Width;
            int height = jpegSizes[0].Height;
            ImageReader reader = ImageReader.NewInstance(width, height, ImageFormatType.Jpeg, 1);
            List<Surface> outputSurfaces = new List<Surface>(1);
            outputSurfaces.Add(reader.Surface);

            CaptureRequest.Builder captureBuilder = cameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
            captureBuilder.AddTarget(reader.Surface);

            File file = new File(GetExternalFilesDir(null), "pic.jpg");
            ImageAvailableListener readerListener = new ImageAvailableListener() { File = file };

            HandlerThread thread = new HandlerThread("CameraPicture");
            thread.Start();
            Handler backgroundHandler = new Handler(thread.Looper);
            reader.SetOnImageAvailableListener(readerListener, backgroundHandler);

            CameraCaptureListener captureListener = new CameraCaptureListener() { Activity = this, File = file };
            cameraDevice.CreateCaptureSession(outputSurfaces, new CameraCaptureStateListener()
            {
                OnConfiguredAction = (CameraCaptureSession session) =>
                {
                    session.Capture(captureBuilder.Build(), captureListener, backgroundHandler);
                }
            }, backgroundHandler);
        }

        private class CameraCaptureListener : CameraCaptureSession.CaptureCallback
        {
            public SessionActivity Activity;
            public File File;
            public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
            {
                base.OnCaptureCompleted(session, request, result);
                Activity activity = Activity;
                if (activity != null)
                {
                    Toast.MakeText(activity, "Saved: " + File.ToString(), ToastLength.Short).Show();
                }
            }
        }

        private class CameraCaptureStateListener : CameraCaptureSession.StateCallback
        {
            public Action<CameraCaptureSession> OnConfiguredAction;
            public override void OnConfigured(CameraCaptureSession session)
            {
                OnConfiguredAction(session);
            }
            public Action<CameraCaptureSession> OnConfigureFailedAction;
            public override void OnConfigureFailed(CameraCaptureSession session)
            {
                OnConfigureFailedAction(session);
            }
        }

        private class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
        {
            public File File;
            public void OnImageAvailable(ImageReader reader)
            {
                Image image = null;
                image = reader.AcquireLatestImage();
                
                ByteBuffer buffer = image.GetPlanes()[0].Buffer;
                byte[] bytes = new byte[buffer.Capacity()];
                buffer.Get(bytes);
                Save(bytes);
            }
            private void Save(byte[] bytes)
            {
                OutputStream output = null;
                try
                {
                    if (File != null)
                    {
                        output = new FileOutputStream(File);
                        output.Write(bytes);
                    }
                }
                finally
                {
                    if (output != null)
                        output.Close();
                }
            }
        }

        private class CameraStateListener : CameraDevice.StateCallback
        {
            private SessionActivity activity;
            public CameraStateListener(SessionActivity acti) { activity = acti; }
            public override void OnOpened(CameraDevice camera)
            {
                activity.cameraDevice = camera;
            }
            public override void OnDisconnected(CameraDevice camera)
            {
                camera.Close();
                activity.cameraDevice = null;
            }

            public override void OnError(CameraDevice camera, [GeneratedEnum] CameraError error)
            {
                camera.Close();
                activity.cameraDevice = null;
            }
        }
    }
}