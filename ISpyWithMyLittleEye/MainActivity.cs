using System;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;
using Java.IO;

namespace ISpyWithMyLittleEye
{
    [Activity(Label = "I Spy With My Little Eye", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, SessionNameDialogFragment.SessionNameDialogListener
    {
        public const string ExtraSessionName = "SESSION_NAME";
        public const string SessionFolderPrefix = "ses_";
        public static string RootPath { get; set; }

        private readonly string[] contextMenuItems = { "Rename", "Delete" };
        private Button addSessionButton;
        private ListView sessionListView;
        private TextView sessionNameDialogTextView;
        private List<string> sessionList;
        private ArrayAdapter<string> sessionListAdapter;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);
            InitializeAll();
        }

        public override void OnCreateContextMenu(IContextMenu menu, View v, IContextMenuContextMenuInfo menuInfo)
        {
            base.OnCreateContextMenu(menu, v, menuInfo);
            if (v.Id == sessionListView.Id)
            {
                AdapterView.AdapterContextMenuInfo info = (AdapterView.AdapterContextMenuInfo)menuInfo;
                menu.SetHeaderTitle(sessionListAdapter.GetItem(info.Position));
                for (int i = 0; i < contextMenuItems.Length; i++)
                {
                    menu.Add(Menu.None, i, i, contextMenuItems[i]);
                }
            }
        }

        public override bool OnContextItemSelected(IMenuItem item)
        {
            AdapterView.AdapterContextMenuInfo info = (AdapterView.AdapterContextMenuInfo)item.MenuInfo;
            string session = sessionListAdapter.GetItem(info.Position);
            //Rename
            if (item.ItemId == 0)
            {
                ShowSessionNameDialog(session);
                GetSessionFolder(session).RenameTo(GetSessionFolder(GetSessionNameDialogText()));
                UpdateSessions();
            }
            //Delete
            if (item.ItemId == 1)
            {
                new AlertDialog.Builder(this).SetTitle("Delete session")
                    .SetMessage("You sure?")
                    .SetPositiveButton("Yeah", (sender, args) =>
                    {
                        GetSessionFolder(session).Delete();
                        UpdateSessions();
                    })
                    .SetNegativeButton("Nope", (sender, args) => { })
                    .Show();
            }
            return base.OnContextItemSelected(item);
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
            InitializeSessionNameDialogTextView();
            InitializeSessionList();
            InitializeNewSessionButton();
        }

        private void InitializeSessionNameDialogTextView()
        {
            var view = ((LayoutInflater)GetSystemService(LayoutInflaterService)).Inflate(Resource.Layout.SessionNameDialog, null);
            sessionNameDialogTextView = view.FindViewById<TextView>(Resource.Id.sessionNameEdit);
        }

        private void InitializeNewSessionButton()
        {
            addSessionButton = FindViewById<Button>(Resource.Id.addSessionButton);
        }

        private void AddSessionButtonClickHandler()
        {
            addSessionButton.Click += (sender, args) =>
            {
                ShowSessionNameDialog();
            };
        }
        
        private void InitializeSessionList()
        {
            InitializeAdapter();
            sessionListView = FindViewById<ListView>(Resource.Id.sessionList);
            sessionListView.Adapter = sessionListAdapter;
            UpdateSessions();
            RegisterForContextMenu(sessionListView);
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

        private void ShowSessionNameDialog(string session = "")
        {
            SetSessionNameDialogText(session);
            new SessionNameDialogFragment().Show(FragmentManager, "session");
        }

        private void AddSession(string name)
        {
            if (name.Length > 0)
            {
                new File(RootPath + "/" + SessionFolderPrefix + name).Mkdir();
                UpdateSessions();
            }
        }

        private void UpdateSessions()
        {
            sessionListAdapter.Clear();
            foreach (File f in new File(RootPath).ListFiles())
            {
                if (f.IsDirectory && f.Name.StartsWith(SessionFolderPrefix))
                {
                    sessionListAdapter.Add(f.Name.Substring(SessionFolderPrefix.Length));
                }
            }
        }

        private File GetSessionFolder(string session)
        {
            return new File(RootPath + "/" + SessionFolderPrefix + session);
        }

        public void OnPositiveButtonClick(DialogFragment dialog)
        {
            AddSession(GetSessionNameDialogText());
            SetSessionNameDialogText();
        }

        public void OnNegativeButtonClick(DialogFragment dialog)
        {
            SetSessionNameDialogText();
        }

        private void SetSessionNameDialogText(string text = "")
        {
            sessionNameDialogTextView.Text = text;
        }

        private string GetSessionNameDialogText()
        {
            return sessionNameDialogTextView.Text;
        }
    }
}