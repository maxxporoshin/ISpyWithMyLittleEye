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
using Android.Graphics;
using Java.Lang;
using Java.Util;

namespace ISpyWithMyLittleEye
{
    class MediaListAdapter : BaseAdapter<string>
    {
        Activity Context { get; set; }
        List<string> Images { get; set; }

        public override int Count
        {
            get
            {
                return Images.Count;
            }
        }

        public override string this[int position]
        {
            get
            {
                return Images[position];
            }
        }

        public MediaListAdapter(Activity context, List<string> images) //: base(context, Resource.Layout.MediaListItem, images)
        {
            Context = context;
            Images = images;
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (convertView == null)
            {
                convertView = Context.LayoutInflater.Inflate(Resource.Layout.MediaListItem, null);
                ImageView imageView = convertView.FindViewById<ImageView>(Resource.Id.image);
                imageView.SetImageBitmap(BitmapFactory.DecodeFile(Images[position]));
            }
            return convertView;
        }

        public override long GetItemId(int position)
        {
            return Images.GetHashCode();
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return Images[position];
        }

        public void Add(string s)
        {
            Images.Add(s);
        }

        public void Clear()
        {
            Images.Clear();
        }
    }
}