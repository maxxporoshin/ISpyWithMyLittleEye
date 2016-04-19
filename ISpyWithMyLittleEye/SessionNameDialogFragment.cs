using System;

using Android.App;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace ISpyWithMyLittleEye
{
	public enum SessionNameDialogType
	{
		Add,
		Rename
	}

    public class SessionNameDialogFragment : DialogFragment
    {
		private string Session;
        private SessionNameDialogListener Listener;
		private SessionNameDialogType Type;
		private View view;
		private TextView sessionNameTextView;

		public SessionNameDialogFragment(string session, SessionNameDialogListener listener, SessionNameDialogType type) : base()
		{
			Session = session;
			Listener = listener;
			Type = type;
		}

        public interface SessionNameDialogListener
        {
			void OnPositiveButtonClick(string name, string oldName, SessionNameDialogType type);
            void OnNegativeButtonClick();
        }
			
        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
			view = ((LayoutInflater)Activity.GetSystemService(Activity.LayoutInflaterService))
				.Inflate(Resource.Layout.SessionNameDialog, null);
			sessionNameTextView = view.FindViewById<TextView>(Resource.Id.sessionNameEdit);
			sessionNameTextView.Text = Session;
			string oldName = Session;
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity);
            builder.SetView(view)
                   .SetPositiveButton("Ok", (sender, args) =>
                    {
						Listener.OnPositiveButtonClick(sessionNameTextView.Text, oldName, Type);
                    })
                   .SetNegativeButton("Nope", (sender, args) =>
                    {
						Listener.OnNegativeButtonClick();
                    });
            return builder.Create();
        }
    }
}