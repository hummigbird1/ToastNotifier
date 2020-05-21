using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace ToastNotifier
{
    internal class SynchronousToastNotificationManager
    {
        private readonly ToastNotification _toastNotification;
        private ManualResetEventSlim _manualResetEventSlim = new ManualResetEventSlim(false);
        private Result _result = Result.Unknown;

        public SynchronousToastNotificationManager(ToastNotification toastNotification)
        {
            _toastNotification = toastNotification;
            _toastNotification.Activated += ToastNotification_Activated;
            _toastNotification.Dismissed += ToastNotification_Dismissed;
            _toastNotification.Failed += ToastNotification_Failed;
        }

        public enum Result
        {
            Unknown,
            Activated,
            Dismissed,
            Dismissed_Timeout,
            Dismissed_ApplicationHidden,
            Dismissed_UserCanceled,
            Failed,
            Cancelled,
        }

        public Exception FailedException { get; private set; }

        public static SynchronousToastNotificationManager FromBuilder(ToastNotificationBuilder toastNotificationBuilder, Action<ToastNotification> configureNotification = null)
        {
            var notification = toastNotificationBuilder.BuildToastNotification();
            configureNotification?.Invoke(notification);
            return new SynchronousToastNotificationManager(notification);
        }

        public Result ShowAndWait(string applicationId, CancellationToken cancellationToken, bool throwExceptionOnFail = true)
        {
            var maanger = ToastNotificationManager.CreateToastNotifier(applicationId);
            maanger.Show(_toastNotification);
            try
            {
                _manualResetEventSlim.Wait(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return Result.Cancelled;
            }

            if (_result == Result.Failed && throwExceptionOnFail)
            {
                if (FailedException != null)
                    throw FailedException;

                throw new Exception("Notification failed for unknown reason!");
            }

            return _result;
        }

        public Task<Result> ShowAndWaitAsync(string applicationId, CancellationToken cancellationToken, bool throwExceptionOnFail = false)
        {
            return new TaskFactory().StartNew(() => ShowAndWait(applicationId, cancellationToken, throwExceptionOnFail), cancellationToken);
        }

        private void ToastNotification_Activated(ToastNotification sender, object args)
        {
            _result = Result.Activated;
            _manualResetEventSlim.Set();
        }

        private void ToastNotification_Dismissed(ToastNotification sender, ToastDismissedEventArgs args)
        {
            _result = Result.Dismissed;
            switch (args.Reason)
            {
                case ToastDismissalReason.ApplicationHidden:
                    _result = Result.Dismissed_ApplicationHidden;
                    break;

                case ToastDismissalReason.TimedOut:
                    _result = Result.Dismissed_Timeout;
                    break;

                case ToastDismissalReason.UserCanceled:
                    _result = Result.Dismissed_UserCanceled;
                    break;
            }

            _manualResetEventSlim.Set();
        }

        private void ToastNotification_Failed(ToastNotification sender, ToastFailedEventArgs args)
        {
            FailedException = args.ErrorCode;
            _result = Result.Failed;
            _manualResetEventSlim.Set();
        }
    }
}