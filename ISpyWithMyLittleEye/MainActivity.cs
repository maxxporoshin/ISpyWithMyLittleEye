using System;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;
using System.IO;

namespace ISpyWithMyLittleEye
{
    [Activity(Label = "I Spy With My Little Eye", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        public const string ExtraSessionName = "SESSION_NAME";
        public const string SessionFolderPrefix = "ses_";
        public static string RootPath { get; set; }

        private Button addSessionButton;
        private ListView sessionListView;
        private TextView newSessionDialogNameEdit;
        private View newSessionDialogView;
        private List<string> sessionList;
        private ArrayAdapter<string> sessionListAdapter;
        private AlertDialog newSessionDialog;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);
            InitializeAll();
        }

        private void InitializeAll()
        {
            SetRootPath(GetExternalFilesDir(null).ToString());
            InitializeUI();
            InitializeEventHandlers();
        }

        private void InitializeEventHandlers()
        {
            AddSessionButtonClickHandler();
            AddSessionListItemClickHandler();
        }

        private void InitializeUI()
        {
            InitializeSessionList();
            CreateNewSessionDialog();
            InitializeNewSessionButton();
        }

        private void InitializeNewSessionButton()
        {
            addSessionButton = FindViewById<Button>(Resource.Id.addSessionButton);
        }

        private void AddSessionButtonClickHandler()
        {
            addSessionButton.Click += (sender, args) =>
            {
                ShowNewSessionDialog();
            };
        }

        private void InitializeSessionList()
        {
            InitializeAdapter();
            sessionListView = FindViewById<ListView>(Resource.Id.sessionList);
            sessionListView.Adapter = sessionListAdapter;
            UpdateSessions();
        }

        private void InitializeAdapter()
        {
            sessionList = new List<String>();
            sessionListAdapter = new ArrayAdapter<String>(this, Android.Resource.Layout.SimpleListItem1, sessionList);
        }

        private void AddSessionListItemClickHandler()
        {
            sessionListView.ItemClick += (sender, args) =>
            {
                StartSessionActivity(args);
            };
        }

        private void SetRootPath(string path)
        {
            RootPath = path;
        }

        private void StartSessionActivity(AdapterView.ItemClickEventArgs args)
        {
            var session = new Intent(this, typeof(SessionActivity));
            session.PutExtra(ExtraSessionName, sessionListAdapter.GetItem(args.Position).ToString());
            StartActivity(session);
        }

        private void ShowNewSessionDialog()
        {
            newSessionDialog.Show();
        }

        private void CreateNewSessionDialog()
        {
            InitializeNewSessionDialogViews();
            AlertDialog.Builder dialogBuilder = CreateNewSessionDialogBuilder();
            newSessionDialog = dialogBuilder.Create();
        }

        private void InitializeNewSessionDialogViews()
        {
            var inflater = (LayoutInflater)GetSystemService(LayoutInflaterService);
            newSessionDialogView = inflater.Inflate(Resource.Layout.AddSessionDialog, null);
            newSessionDialogNameEdit = newSessionDialogView.FindViewById<TextView>(Resource.Id.sessionNameEdit);
        }

        private AlertDialog.Builder CreateNewSessionDialogBuilder()
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            ConfigureNewSessionDialogBuilder(builder);
            return builder;
        }

        private void ConfigureNewSessionDialogBuilder(AlertDialog.Builder builder)
        {
            builder.SetView(newSessionDialogView);
            SetNewSessionDialogBuilderPositive(builder);
            SetNewSessionDialogBuilderNegative(builder);
        }

        private void SetNewSessionDialogBuilderNegative(AlertDialog.Builder builder)
        {
            builder.SetNegativeButton("Nope", (sender, args) =>
            {
                ClearNewSessionDialogNameEdit();
            });
        }

        private void SetNewSessionDialogBuilderPositive(AlertDialog.Builder builder)
        {
            builder.SetPositiveButton("Ok", (sender, args) =>
            {
                AddSession(newSessionDialogNameEdit.Text);
                ClearNewSessionDialogNameEdit();
            });
        }

        private void ClearNewSessionDialogNameEdit()
        {
            newSessionDialogNameEdit.Text = "";
        }

        private void AddSession(string name)
        {
            if (name.Length > 0)
            {
                sessionListAdapter.Add(name);
                Directory.CreateDirectory(RootPath + "/" + SessionFolderPrefix + name);
            }
        }

        private void UpdateSessions()
        {
            sessionListAdapter.Clear();
            foreach (string s in Directory.GetDirectories(RootPath))
            {
                AddSessionFromDirectory(Path.GetFileName(s));
            }
        }

        private void AddSessionFromDirectory(string directory)
        {
            if (directory.StartsWith(SessionFolderPrefix))
            {
                AddSession(directory.Substring(SessionFolderPrefix.Length));
            }
        }
    }
}