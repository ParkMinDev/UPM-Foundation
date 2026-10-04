using System;
using System.Threading;
using ParkMinDev.UPM.Foundation.Extensions;

namespace ParkMinDev.UPM.Foundation.Objects.Threading
{
	public sealed class AutoRenewCancellationTokenSource : IDisposable
	{
		// - Public Methods -
		public CancellationToken CancelPreviousAndCreateToken(
			CancellationToken cancellationToken = default
		) {
			CancellationTokenSource nextCancellationTokenSource = cancellationToken.CanBeCanceled
				? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
				: new CancellationTokenSource();
			CancellationToken nextCancellationToken = nextCancellationTokenSource.Token;
			CancellationTokenSource previousCancellationTokenSource;

			lock (_gate) {
				if (_isDisposed) {
					nextCancellationTokenSource.Dispose();
					throw new ObjectDisposedException(nameof(AutoRenewCancellationTokenSource));
				}
				previousCancellationTokenSource = _cancellationTokenSource;
				_cancellationTokenSource = nextCancellationTokenSource;
			}

			previousCancellationTokenSource.CancelAndDispose();
			return nextCancellationToken;
		}

		public void Cancel() {
			CancellationTokenSource cancellationTokenSource;

			lock (_gate) {
				if (_isDisposed) {
					throw new ObjectDisposedException(nameof(AutoRenewCancellationTokenSource));
				}
				cancellationTokenSource = _cancellationTokenSource;
				_cancellationTokenSource = null;
			}

			cancellationTokenSource.CancelAndDispose();
		}

		public void Dispose() {
			CancellationTokenSource cancellationTokenSource;

			lock (_gate) {
				if (_isDisposed) {
					return;
				}
				_isDisposed = true;
				cancellationTokenSource = _cancellationTokenSource;
				_cancellationTokenSource = null;
			}

			cancellationTokenSource.CancelAndDispose();
		}

		// - Internals -
		readonly object _gate = new object();
		CancellationTokenSource _cancellationTokenSource;
		bool _isDisposed;
	}
}