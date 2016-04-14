............................................................................файл.cs
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
using Android.Hardware;
using Android.Graphics;
using Android.Media;
using Java.IO;

namespace TakePhoto
{
    [Activity(Label = "TakePhotoActivity")]
    public class TakePhotoActivity : Activity, ISurfaceHolderCallback, Android.Hardware.Camera.IPictureCallback
    {
        private Android.Hardware.Camera camera;
        private ISurfaceHolder surfaceHolder;
        private bool previewing = false;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.takePhotoLayout);
            // Create your application here
            SurfaceView surfaceView = FindViewById<SurfaceView>(Resource.Id.surfaceView1);
            surfaceHolder = surfaceView.Holder;
            surfaceHolder.AddCallback(this);
            surfaceHolder.SetType(SurfaceType.PushBuffers);

            var takePictureButton = FindViewById<ImageButton>(Resource.Id.imageButton1);
            takePictureButton.Click += delegate
            {
                try
                {
                    camera.TakePicture(null, null, this);
                }
                catch { };
            };
            var mes = FindViewById<EditText>(Resource.Id.textMessage);
            mes.AfterTextChanged += delegate
            {
                Notes.NotesList.FindLast(x => x.location == null).Text = mes.Text;
            };
            Notes.NotesList.Add(new Note() { Text = "No name"});
        }
        public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
        {
            // TODO Auto-generated method stub
            if (previewing)
            {
                camera.StopPreview();
                previewing = false;
            }
            if (camera != null)
            {
                try
                {
                    camera.SetPreviewDisplay(surfaceHolder);
                    camera.StartPreview();
                    previewing = true;
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
            // TODO Auto-generated method stub
            camera = Android.Hardware.Camera.Open();
        }
        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            // TODO Auto-generated method stub
            camera.StopPreview();
            camera.Release();
            camera = null;
            previewing = false;
        }

        public void OnPictureTaken(byte[] data, Android.Hardware.Camera camera)
        {

            Bitmap bitmapPicture = BitmapFactory.DecodeByteArray(data, 0, data.Length);
            bitmapPicture = Bitmap.CreateScaledBitmap(bitmapPicture, bitmapPicture.Width / 5, bitmapPicture.Height / 5, false);
            //и сохраняете как вам угодно
        }
    }
}
...................................................................................................

