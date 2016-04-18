using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Graphics;
using Android.Media;
using Java.IO;
using Java.Nio;
using Java.Lang;
using CameraError = Android.Hardware.Camera2.CameraError;

namespace ISpyWithMyLittleEye
{
    [Activity(Label = "Session Activity")]
    public class SessionActivity : Activity
    {
        private List<string> images;
        private MediaListAdapter adapter;
        private ListView mediaList;
        private Button photoButton;
        private Button videoButton;
        private string sessionPath;
        private CameraDevice cameraDevice;
        private bool openingCamera;
        private CameraStateListener stateListener;
        private HandlerThread thread;
        private Handler backgroundHandler;
        private MediaRecorder mediaRecorder;
        private bool recordingVideo;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SessionActivityLayout);
            InitializeAll();
        }

        private void InitializeAll()
        {
            stateListener = new CameraStateListener(this);
            sessionPath = MainActivity.RootPath + "/ses_" + Intent.GetStringExtra(MainActivity.ExtraSessionName) + "/";
            InitializeUI();
        }

        private void InitializeUI()
        {
            photoButton = FindViewById<Button>(Resource.Id.photoButton);
            videoButton = FindViewById<Button>(Resource.Id.videoButton);
            InitializeEventHandlers();
            InitializeSessionList();
        }

        private void InitializeEventHandlers()
        {
            photoButton.Click += (sender, args) => { TakePicture(); };
            videoButton.Click += (sender, args) =>
            {
                if (recordingVideo)
                {
                    StopRecord();
                }
                else
                {
                    StartRecord();

                }
            };
        }

        private void StopRecord()
        {
            //mediaRecorder.Stop();
            //mediaRecorder.Reset();
            CloseCamera();
            OpenCamera();
            videoButton.Text = "Start record";
            recordingVideo = false;
        }

        private void InitializeSessionList()
        {
            InitializeAdapter();
            mediaList = FindViewById<ListView>(Resource.Id.mediaList);
            mediaList.Adapter = adapter;
            UpdateMediaList();
        }

        private void InitializeAdapter()
        {
            images = new List<string>();
            adapter = new MediaListAdapter(this, images);
        }

        protected override void OnResume()
        {
            base.OnResume();
            StartBackgroundThread();
            OpenCamera();
        }
        protected override void OnPause()
        {
            base.OnPause();
            CloseCamera();
            StopBackgroundThread();
        }

        private void CloseCamera()
        {
            CloseCameraDevice();
            ReleaseMediaRecorder();
        }

        private void ReleaseMediaRecorder()
        {
            if (mediaRecorder != null)
            {
                mediaRecorder.Release();
                mediaRecorder = null;
            }
        }

        private void CloseCameraDevice()
        {
            if (cameraDevice != null)
            {
                cameraDevice.Close();
                cameraDevice = null;
            }
        }

        private class CameraStateListener : CameraDevice.StateCallback
        {
            public SessionActivity Activity;
            public CameraStateListener(SessionActivity context)
            {
                Activity = context;
            }
            public override void OnOpened(CameraDevice camera)
            {

                if (Activity != null)
                {
                    Activity.cameraDevice = camera;
                    Activity.openingCamera = false;
                }
            }

            public override void OnDisconnected(CameraDevice camera)
            {
                if (Activity != null)
                {
                    camera.Close();
                    Activity.cameraDevice = null;
                    Activity.openingCamera = false;
                }
            }

            public override void OnError(CameraDevice camera, CameraError error)
            {
                camera.Close();
                if (Activity != null)
                {
                    Activity.cameraDevice = null;
                    Activity.openingCamera = false;
                }

            }
        }
        private class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
        {
            public File File;
            public void OnImageAvailable(ImageReader reader)
            {
                Image image = null;
                try
                {
                    image = reader.AcquireLatestImage();
                    ByteBuffer buffer = image.GetPlanes()[0].Buffer;
                    byte[] bytes = new byte[buffer.Capacity()];
                    buffer.Get(bytes);
                    Save(bytes);
                }
                catch (FileNotFoundException ex)
                {
                    Log.WriteLine(LogPriority.Info, "Camera capture session", ex.StackTrace);
                }
                catch (IOException ex)
                {
                    Log.WriteLine(LogPriority.Info, "Camera capture session", ex.StackTrace);
                }
                finally
                {
                    if (image != null)
                        image.Close();
                }
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
        private class CameraCaptureListener : CameraCaptureSession.CaptureCallback
        {
            public SessionActivity Activity;
            public File File;
            public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
            {
                if (Activity != null && File != null)
                {
                    Activity.AddImage(File.ToString());
                    new UpdateMediaListTask(Activity.adapter).Execute();
                }
            }
        }
        private class CameraCaptureStateListener : CameraCaptureSession.StateCallback
        {
            public Action<CameraCaptureSession> OnConfigureFailedAction;
            public override void OnConfigureFailed(CameraCaptureSession session)
            {
                OnConfigureFailedAction(session);
            }

            public Action<CameraCaptureSession> OnConfiguredAction;
            public override void OnConfigured(CameraCaptureSession session)
            {
                OnConfiguredAction(session);
            }

        }
        private class CameraCapturerRecordStateListener : CameraCaptureSession.StateCallback
        {
            public Action<CameraCaptureSession> OnConfigureFailedAction;
            public override void OnConfigureFailed(CameraCaptureSession session)
            {
                OnConfigureFailedAction(session);
            }

            public Action<CameraCaptureSession> OnConfiguredAction;
            public override void OnConfigured(CameraCaptureSession session)
            {
                OnConfiguredAction(session);
            }

        }
        private class UpdateMediaListTask : AsyncTask
        {
            private MediaListAdapter Adapter;
            public UpdateMediaListTask(MediaListAdapter adapter)
            {
                Adapter = adapter;
            }
            protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] @params)
            {
                return null;
            }

            protected override void OnPostExecute(Java.Lang.Object result)
            {
                base.OnPostExecute(result);
                Adapter.NotifyDataSetChanged();
            }
        }

        private void StartBackgroundThread()
        {
            thread = new HandlerThread("CameraThread");
            thread.Start();
            backgroundHandler = new Handler(thread.Looper);
        }

        private void StopBackgroundThread()
        {
            thread.QuitSafely();
            try
            {
                thread.Join();
                thread = null;
                backgroundHandler = null;
            } catch (InterruptedException e)
            {
                e.PrintStackTrace();
            }
        }

        void UpdateMediaList()
        {
            adapter.Clear();
            foreach (File f in (new File(sessionPath)).ListFiles())
            {
                if (f.Name.EndsWith(".jpg"))
                {
                    adapter.Add(f.ToString());
                }
            }
            adapter.NotifyDataSetChanged();
        }
        private void OpenCamera()
        {
            if (openingCamera)
            {
                return;
            }
            mediaRecorder = new MediaRecorder();
            openingCamera = true;
            CameraManager manager = (CameraManager)GetSystemService(CameraService);
            try
            {
                string cameraId = manager.GetCameraIdList()[0];
                CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraId);
                StreamConfigurationMap map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                manager.OpenCamera(cameraId, stateListener, backgroundHandler);
            }
            catch (CameraAccessException ex)
            {
                Toast.MakeText(this, "Cannot access the camera.", ToastLength.Short).Show();
            }
        }
        private void SetUpCaptureRequestBuilder(CaptureRequest.Builder builder)
        {
            builder.Set(CaptureRequest.ControlMode, new Integer((int)ControlMode.Auto));
        }
        private void TakePicture()
        {
            try
            {
                if (cameraDevice == null)
                {
                    return;
                }
                CameraManager manager = (CameraManager)GetSystemService(Context.CameraService);

                CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraDevice.Id);
                Size[] jpegSizes = null;
                if (characteristics != null)
                {
                    jpegSizes = ((StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap)).GetOutputSizes((int)ImageFormatType.Jpeg);
                }
                int width = 640;
                int height = 480;
                if (jpegSizes != null && jpegSizes.Length > 0)
                {
                    width = jpegSizes[0].Width;
                    height = jpegSizes[0].Height;
                }

                ImageReader reader = ImageReader.NewInstance(width, height, ImageFormatType.Jpeg, 1);
                List<Surface> outputSurfaces = new List<Surface>();
                outputSurfaces.Add(reader.Surface);

                CaptureRequest.Builder captureBuilder = cameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
                captureBuilder.AddTarget(reader.Surface);
                SetUpCaptureRequestBuilder(captureBuilder);

                DateTime now = DateTime.Now;
                string filePath = sessionPath + "-" + now.Day.ToString() + "." +  now.Month.ToString() + "." + now.Year.ToString() + "-" 
                    + now.Hour.ToString() + "_" + now.Minute.ToString() + "_" + now.Second.ToString() + ".jpg";
                File file = new File(filePath);

                ImageAvailableListener readerListener = new ImageAvailableListener() { File = file };
                reader.SetOnImageAvailableListener(readerListener, backgroundHandler);
                CameraCaptureListener captureListener = new CameraCaptureListener() { Activity = this, File = file };

                cameraDevice.CreateCaptureSession(outputSurfaces, new CameraCaptureStateListener()
                {
                    OnConfiguredAction = (CameraCaptureSession session) => {
                        try
                        {
                            session.Capture(captureBuilder.Build(), captureListener, backgroundHandler);
                        }
                        catch (CameraAccessException ex)
                        {
                            Log.WriteLine(LogPriority.Info, "Capture Session error: ", ex.ToString());
                        }
                    }
                }, backgroundHandler);
            }
            catch (CameraAccessException ex)
            {
                Log.WriteLine(LogPriority.Info, "Taking picture error: ", ex.StackTrace);
            }
        }
        private void SetupMediaRecorder()
        {
            mediaRecorder.SetVideoSource(VideoSource.Surface);
            mediaRecorder.SetOutputFormat(OutputFormat.Mpeg4);
            mediaRecorder.SetOutputFile(new File(sessionPath + "/video.mp4").AbsolutePath);
            mediaRecorder.SetVideoEncodingBitRate(10000000);
            mediaRecorder.SetVideoFrameRate(30);
            mediaRecorder.SetVideoSize(640, 480);
            mediaRecorder.SetVideoEncoder(VideoEncoder.H264);              
            mediaRecorder.Prepare();
        }
        private void StartRecord()
        {
            SetupMediaRecorder();
            Surface recordSurface = mediaRecorder.Surface;
            CaptureRequest.Builder builder = cameraDevice.CreateCaptureRequest(CameraTemplate.Record);
            builder.AddTarget(recordSurface);
            SetUpCaptureRequestBuilder(builder);
            List<Surface> surfaces = new List<Surface>();
            surfaces.Add(recordSurface);
            cameraDevice.CreateCaptureSession(surfaces, new CameraCapturerRecordStateListener()
            {
                OnConfiguredAction = (CameraCaptureSession session) =>
                {
                    try
                    {
                        session.SetRepeatingRequest(builder.Build(), null, backgroundHandler);
                    }
                    catch (CameraAccessException ex)
                    {
                        Log.WriteLine(LogPriority.Info, "Capture Session error: ", ex.ToString());
                    }
                }
            }, backgroundHandler);
            mediaRecorder.Start();
            videoButton.Text = "Stop record";
            recordingVideo = true;
        }
        public void AddImage(string path)
        {
            adapter.Add(path);
        }
    }
}