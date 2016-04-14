using System;
using System.Collections.Generic;

using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;
using Android.Media;
using Java.IO;
using Java.Nio;
using Android.Hardware;

namespace ISpyWithMyLittleEye
{
    [Activity(Label = "SessionActivity")]
    public class SessionActivity : Activity
    {
		Camera camera;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.SessionActivityLayout);
            var spyButton = FindViewById<Button>(Resource.Id.spyButton);
	
            spyButton.Click += (sender, args) =>
            {
				TakePicture();
            };
            
        }
			
		protected void TakePicture()
        {
			camera = Camera.Open(0);
			camera.SetPreviewDisplay((new SurfaceView(this)).Holder);
			camera.StartPreview();
			File file = new File(MainActivity.RootPath + "/hui.png");
			PictureCallback pictureCallback = new PictureCallback(){ File = file };
			camera.TakePicture(null, null, pictureCallback);
        }

		private class PictureCallback : Camera.IPictureCallback 
		{
			public File File;
			public void OnPictureTaken (byte[] data, Camera camera)
			{
				OutputStream output = null;
				try
				{
					if (File != null)
					{
						output = new FileOutputStream(File);
						output.Write(data);
					}
				}
				finally
				{
					camera.StopPreview();
					camera.Release();
					camera = null;
					if (output != null)
						output.Close();
				}
			}
			public void Dispose ()
			{
			}

			public IntPtr Handle {
				get;
			}
		}
    }
}