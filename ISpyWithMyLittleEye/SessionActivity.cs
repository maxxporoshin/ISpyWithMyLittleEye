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
        private string sessionPath;
        // A reference to the opened CameraDevice
        private CameraDevice cameraDevice;
        // True if the app is currently trying to open the camera
        private bool openingCamera;
        // CameraDevice.StateListener is called when a CameraDevice changes its state
        private CameraStateListener stateListener;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SessionActivityLayout);
            stateListener = new CameraStateListener() { Activity = this };
            FindViewById<Button>(Resource.Id.spyButton).Click += (sender, args) => { TakePicture(); };
            images = new List<string>();
            adapter = new MediaListAdapter(this, images);
            mediaList = FindViewById<ListView>(Resource.Id.mediaList);
            mediaList.Adapter = adapter;
            UpdateMediaList();

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

        private class CameraStateListener : CameraDevice.StateCallback
        {
            public SessionActivity Activity;
            public override void OnOpened(CameraDevice camera)
            {

                if (Activity != null)
                {
                    Activity.cameraDevice = camera;
                    //Activity.StartPreview();
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
                    Activity activity = Activity;
                    Activity.openingCamera = false;
                    if (activity != null)
                    {
                        activity.Finish();
                    }
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
            public SessionActivity Fragment;
            public File File;
            public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
            {
                if (Fragment != null && File != null)
                {
                    Activity activity = Fragment;
                    if (activity != null)
                    {
                        Toast.MakeText(activity, "Saved: " + File.ToString(), ToastLength.Short).Show();
                        Fragment.AddImage(File.ToString());
                        new UpdateMediaListTask(Fragment.adapter).Execute();
                        //Fragment.StartPreview();
                    }
                }
            }
        }
        // This CameraCaptureSession.StateListener uses Action delegates to allow the methods to be defined inline, as they are defined more than once
        private class CameraCaptureStateListener : CameraCaptureSession.StateCallback
        {
            public Action<CameraCaptureSession> OnConfigureFailedAction;
            public override void OnConfigureFailed(CameraCaptureSession session)
            {
                if (OnConfigureFailedAction != null)
                {
                    OnConfigureFailedAction(session);
                }
            }

            public Action<CameraCaptureSession> OnConfiguredAction;
            public override void OnConfigured(CameraCaptureSession session)
            {
                    OnConfiguredAction(session);
            }

        }
        public class ErrorDialog : DialogFragment
        {
            public override Dialog OnCreateDialog(Bundle savedInstanceState)
            {
                var alert = new AlertDialog.Builder(Activity);
                alert.SetMessage("This device doesn't support Camera2 API.");
                alert.SetPositiveButton(Android.Resource.String.Ok, new MyDialogOnClickListener(this));
                return alert.Show();

            }
        }
        private class MyDialogOnClickListener : Java.Lang.Object, IDialogInterfaceOnClickListener
        {
            ErrorDialog er;
            public MyDialogOnClickListener(ErrorDialog e)
            {
                er = e;
            }
            public void OnClick(IDialogInterface dialogInterface, int i)
            {
                er.Activity.Finish();
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

        void UpdateMediaList()
        {
            sessionPath = MainActivity.RootPath + "/ses_" + Intent.GetStringExtra(MainActivity.ExtraSessionName) + "/";
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
        // Opens a CameraDevice. The result is listened to by 'mStateListener'.
        private void OpenCamera()
        {
            Activity activity = this;
            if (activity == null || activity.IsFinishing || openingCamera)
            {
                return;
            }
            openingCamera = true;
            CameraManager manager = (CameraManager)activity.GetSystemService(Context.CameraService);
            try
            {
                string cameraId = manager.GetCameraIdList()[0];

                // To get a list of available sizes of camera preview, we retrieve an instance of
                // StreamConfigurationMap from CameraCharacteristics
                CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraId);
                StreamConfigurationMap map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);

                // We are opening the camera with a listener. When it is ready, OnOpened of mStateListener is called.
                manager.OpenCamera(cameraId, stateListener, null);
            }
            catch (CameraAccessException ex)
            {
                Toast.MakeText(activity, "Cannot access the camera.", ToastLength.Short).Show();
            }
            catch (NullPointerException)
            {
                var dialog = new ErrorDialog();
                dialog.Show(FragmentManager, "dialog");
            }
        }
        // Sets up capture request builder.
        private void SetUpCaptureRequestBuilder(CaptureRequest.Builder builder)
        {
            // In this sample, w just let the camera device pick the automatic settings
            builder.Set(CaptureRequest.ControlMode, new Java.Lang.Integer((int)ControlMode.Auto));
        }
        // Takes a picture.
        private void TakePicture()
        {
            try
            {
                if (cameraDevice == null)
                {
                    return;
                }
                CameraManager manager = (CameraManager)GetSystemService(Context.CameraService);

                // Pick the best JPEG size that can be captures with this CameraDevice
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

                // We use an ImageReader to get a JPEG from CameraDevice
                // Here, we create a new ImageReader and prepare its Surface as an output from the camera
                ImageReader reader = ImageReader.NewInstance(width, height, ImageFormatType.Jpeg, 1);
                List<Surface> outputSurfaces = new List<Surface>();
                outputSurfaces.Add(reader.Surface);
                //outputSurfaces.Add(new Surface(textureView.SurfaceTexture));

                CaptureRequest.Builder captureBuilder = cameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
                captureBuilder.AddTarget(reader.Surface);
                SetUpCaptureRequestBuilder(captureBuilder);

                // Output file
                DateTime now = DateTime.Now;
                string filePath = sessionPath + now.Day.ToString() + "." +  now.Month.ToString() + "." + now.Year.ToString() + "-" 
                    + now.Hour.ToString() + "_" + now.Minute.ToString() + "_" + now.Second.ToString() + ".jpg";
                File file = new File(filePath);

                // This listener is called when an image is ready in ImageReader 
                // Right click on ImageAvailableListener in your IDE and go to its definition
                ImageAvailableListener readerListener = new ImageAvailableListener() { File = file };

                // We create a Handler since we want to handle the resulting JPEG in a background thread
                HandlerThread thread = new HandlerThread("CameraPicture");
                thread.Start();
                Handler backgroundHandler = new Handler(thread.Looper);
                reader.SetOnImageAvailableListener(readerListener, backgroundHandler);

                //This listener is called when the capture is completed
                // Note that the JPEG data is not available in this listener, but in the ImageAvailableListener we created above
                // Right click on CameraCaptureListener in your IDE and go to its definition
                CameraCaptureListener captureListener = new CameraCaptureListener() { Fragment = this, File = file };

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
        public void AddImage(string path)
        {
            adapter.Add(path);
        }
    }
}