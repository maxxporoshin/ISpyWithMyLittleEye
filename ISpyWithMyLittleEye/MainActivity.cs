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
				ShowSessionNameDialog(SessionNameDialogType.Rename, session);
            }
            //Delete
            if (item.ItemId == 1)
            {
                new AlertDialog.Builder(this).SetTitle("Delete session")
                    .SetMessage("You sure?")
                    .SetPositiveButton("Yes", (sender, e) =>
                    {
						DeleteSession(session);
                    })
                    .SetNegativeButton("Nope", (sender, e) => { })
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
            CreateSessionListItemClickHandler();
        }

        private void InitializeUI()
        {
            InitializeSessionList();
            InitializeNewSessionButton();
        }

        private void InitializeNewSessionButton()
        {
            addSessionButton = FindViewById<Button>(Resource.Id.addSessionButton);
        }

        private void AddSessionButtonClickHandler()
        {
            addSessionButton.Click += (sender, e) =>
            {
				ShowSessionNameDialog(SessionNameDialogType.Add);
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

        private void CreateSessionListItemClickHandler()
        {
            sessionListView.ItemClick += (sender, e) =>
            {
                StartSessionActivity(e);
            };
        }

        private void SetRootPath(string path)
        {
            RootPath = path;
        }

        private void StartSessionActivity(AdapterView.ItemClickEventArgs e)
        {
            var session = new Intent(this, typeof(SessionActivity));
            session.PutExtra(ExtraSessionName, sessionListAdapter.GetItem(e.Position).ToString());
            StartActivity(session);
        }

		private void ShowSessionNameDialog(SessionNameDialogType type, string session = "")
        {
			new SessionNameDialogFragment(session, this, type).Show(FragmentManager, "session");
        }

		private bool AddSession(string session)
        {
			File file = GetSessionFolder(session);
			if (session.Length == 0 || file.Exists())
            {
				return false;
            }
			file.Mkdir();
			UpdateSessions();
			return true;
        }

		private bool RenameSession(string session, string to)
		{
			File file = GetSessionFolder(to);
			if (file.Exists()) 
			{
				return false;
			} 
			GetSessionFolder(session).RenameTo(file);
			UpdateSessions();
			return true;
		}

		private void DeleteSession(string session)
		{
			File folder = GetSessionFolder(session);
			foreach (File file in folder.ListFiles())
			{
				file.Delete();
			}
			folder.Delete();
			UpdateSessions();
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

		public void OnPositiveButtonClick(string name, string oldName, SessionNameDialogType type)
        {
			if (type == SessionNameDialogType.Add) 
			{
				AddSession(name);
			}
			if (type == SessionNameDialogType.Rename)
			{
				RenameSession(oldName, name);
			}
        }

        public void OnNegativeButtonClick()
        {
        }
    }
}