using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;
using Android.Media;
using Java.IO;
using Java.Nio;
using Android.Hardware;
using Java.Lang;

namespace ISpyWithMyLittleEye
{
    [Activity(Label = "SessionActivity")]
    public class SessionActivity : Activity
    {
		Camera camera;
        CameraPreview preview;
        MediaRecorder recorder;
        ISurfaceHolder surfaceHolder;
        private bool previewing = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.SessionActivityLayout);
            camera = Camera.Open();
            preview = new CameraPreview(this, camera);
            SurfaceView surfaceView = FindViewById<SurfaceView>(Resource.Id.dummySurfaceView);
            surfaceHolder = surfaceView.Holder;
            surfaceHolder.AddCallback(new CameraPreview(this, camera));
            surfaceHolder.SetType(SurfaceType.PushBuffers);
            var spyButton = FindViewById<Button>(Resource.Id.spyButton);
	
            spyButton.Click += (sender, args) =>
            {
				TakePicture();
            };
            
        }

        protected override void OnPause()
        {
            base.OnPause();
            ReleaseMediaRecorder();
            ReleaseCamera();
        }
        protected void TakePicture()
        {
			camera.TakePicture(null, null, new PictureCallback());
        }

        private bool PrepareMediaRecorder()
        {
            camera = Camera.Open();
            recorder = new MediaRecorder();

            camera.Unlock();
            recorder.SetCamera(camera);

            recorder.SetAudioSource(AudioSource.Camcorder);
            recorder.SetVideoSource(VideoSource.Camera);

            recorder.SetProfile(CamcorderProfile.Get(CamcorderQuality.High));

            recorder.SetOutputFile(GetOutputMediaFile(MediaType.Video).ToString());

            recorder.SetPreviewDisplay(preview.Holder.Surface);

            try
            {
                recorder.Prepare();
            }
            catch (IllegalStateException e)
            {
                Log.Debug("PrepareMediaRecorder", "Illegal state: " + e.Message);
                ReleaseMediaRecorder();
                return false;
            }
            catch (IOException e)
            {
                Log.Debug("PrepareMediaRecorder", "IOException " + e.Message);
                ReleaseMediaRecorder();
                return false;
            }

            return true;
        }

        private void ReleaseMediaRecorder()
        {
            if(recorder != null)
            {
                recorder.Reset();
                recorder.Release();
                recorder = null;
                camera.Lock();
            }
        }

        private void ReleaseCamera()
        {
            if (camera != null)
            {
                camera.Release();
                camera = null;
            }
        }

        private static Android.Net.Uri GetOutputMediaFileUri(MediaType type)
        {
            return Android.Net.Uri.FromFile(GetOutputMediaFile(type));
        }

        private static File GetOutputMediaFile(MediaType type)
        {
            File mediaStorageDir = new File(MainActivity.RootPath);
            File mediaFile = null;
            switch (type)
            {
                case MediaType.Image:
                    mediaFile = new File(mediaStorageDir.Path + File.Separator + "hui.jpg");
                    break;
                case MediaType.Video:
                    mediaFile = new File(mediaStorageDir.Path + File.Separator + "hui.mp4");
                    break;

            }
            return mediaFile;
        }

        private enum MediaType
        {
            Video,
            Image
        }

        private class PictureCallback : Camera.IPictureCallback 
		{
			public void OnPictureTaken(byte[] data, Camera camera)
			{
                File pictureFile = SessionActivity.GetOutputMediaFile(MediaType.Image);
                if (pictureFile == null)
                {
                    Log.Debug("OnPictureTaken: pictureFile", "error creating media file");
                    return;
                }

                try
                {
                    FileOutputStream fos = new FileOutputStream(pictureFile);
                    fos.Write(data);
                    fos.Close();
                    Log.Debug("OnPictureTaken", "Success");
                }
                catch (FileNotFoundException e)
                {
                    Log.Debug("OnPictureTaken: fos", "File not found: " + e.Message);
                }
                catch (IOException e)
                {
                    Log.Debug("OnPictureTaken fos", "Error accessing file: " + e.Message);
                }
                
			}

			public void Dispose()
			{
			}

			public IntPtr Handle {
				get;
			}
		}

        private class CameraPreview : SurfaceView, ISurfaceHolderCallback
        {
            private Camera camera;
            private SessionActivity activity;

            public CameraPreview(Context context, Camera camera) : base(context)
            {
                this.camera = camera;
                Holder.AddCallback(this);
                activity = (SessionActivity)context;
            }

            public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Android.Graphics.Format format, int width, int height)
            {
                if (activity.previewing)
                {
                    camera.StopPreview();
                    activity.previewing = false;
                }
                if (camera != null)
                {
                    try
                    {
                        camera.SetPreviewDisplay(activity.surfaceHolder);
                        camera.StartPreview();
                        activity.previewing = true;
                    }
                    catch (IOException e)
                    {
                        // TODO Auto-generated catch block
                        e.PrintStackTrace();
                    }
                }
            }

            public void SurfaceCreated(ISurfaceHolder holder)
            {
                camera = Camera.Open();
            }

            public void SurfaceDestroyed(ISurfaceHolder holder)
            {
                camera.StopPreview();
                camera.Release();
                camera = null;
                activity.previewing = false;
            }
        }
    }
}