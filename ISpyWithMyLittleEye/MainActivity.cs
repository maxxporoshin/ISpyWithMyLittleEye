using System;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ISpyWithMyLittleEye
{
    [Activity(Label = "I Spy With My Little Eye", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        Button addSessionButton;
        ListView sessionList;
        TextView nameEdit;
        List<String> sessions;
        static public string RootPath { get; set; }
        
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            
            RootPath = GetExternalFilesDir(null).ToString();

            sessionList = FindViewById<ListView>(Resource.Id.sessionList);

            var serializer = new XmlSerializer(typeof(List<String>));
            Stream reader = new FileStream(RootPath, FileMode.Open);
            sessions = (List<String>)serializer.Deserialize(reader);
            reader.Close();

            var adapter = new ArrayAdapter<String>(this, Android.Resource.Layout.SimpleListItem1, sessions);
            sessionList.Adapter = adapter;

            var inflater = (LayoutInflater)GetSystemService(LayoutInflaterService);
            var view = inflater.Inflate(Resource.Layout.AddSessionDialog, null);
            nameEdit = view.FindViewById<TextView>(Resource.Id.sessionNameEdit);
            var dialogBuilder = new AlertDialog.Builder(this)
                .SetView(view)
                .SetPositiveButton("Ok", (sender, args) => {
                    string name = nameEdit.Text;
                    adapter.Add(name);
                    nameEdit.Text = "";
                    Directory.CreateDirectory(RootPath + "/" + name);
                })
                .SetNegativeButton("Nope", (sender, args) => {
                    nameEdit.Text = "";
                });
            var dialog = dialogBuilder.Create();
            addSessionButton = FindViewById<Button>(Resource.Id.addSessionButton);
            addSessionButton.Click += (sender, args) =>
            {
                dialog.Show();
            };
            sessionList.ItemClick += (sender, args) => {
                string item = adapter.GetItem(args.Position);
                var session = new Intent(this, typeof(SessionActivity));
                StartActivity(session);
            };
        }

        protected override void OnPause()
        {
            base.OnPause();

        }
    }
}