using System;

using Android.App;
using Android.OS;
using Android.Util;
using Android.Views;

namespace ISpyWithMyLittleEye
{
    public class SessionNameDialogFragment : DialogFragment
    {
        private SessionNameDialogListener listener;

        public interface SessionNameDialogListener
        {
            void OnPositiveButtonClick(DialogFragment dialog);
            void OnNegativeButtonClick(DialogFragment dialog);
        }

        [Obsolete]
        public override void OnAttach(Activity activity)
        {
            base.OnAttach(activity);
            try
            {
                listener = (SessionNameDialogListener)activity;
            }
            catch (InvalidCastException ex)
            {
                Log.WriteLine(LogPriority.Info, "Session name dialog attach", ex.StackTrace);
            }
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity);
            builder.SetView(((LayoutInflater)Activity.GetSystemService(Activity.LayoutInflaterService)).Inflate(Resource.Layout.SessionNameDialog, null))
                   .SetPositiveButton("Ok", (sender, args) =>
                    {
                        listener.OnPositiveButtonClick(this);
                    })
                   .SetNegativeButton("Nope", (sender, args) =>
                    {
                        listener.OnNegativeButtonClick(this);
                    });
            return builder.Create();
        }
    }
}