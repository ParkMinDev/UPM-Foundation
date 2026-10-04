using System;
using System.Threading;
using UnityEngine;

namespace ParkMinDev.UPM.Foundation.Expansions
{
	public static class AwaitableExpansions
	{
		public static async Awaitable Run(Func<Awaitable> asyncFunc) {
			await asyncFunc();
		}

		public static async Awaitable WaitUntil(Func<bool> predicate, CancellationToken cancellationToken) {
			// 조건을 만족할 때까지 매 프레임 검사
			while (!predicate()) {
				cancellationToken.ThrowIfCancellationRequested();
				await Awaitable.NextFrameAsync(cancellationToken);
			}
		}
		public static async Awaitable WaitWhile(Func<bool> predicate, CancellationToken cancellationToken) {
			while (predicate()) {
				cancellationToken.ThrowIfCancellationRequested();
				await Awaitable.NextFrameAsync(cancellationToken);
			}
		}
		public static Awaitable WhenAll(params Awaitable[] awaitables) {
			if (awaitables == null || awaitables.Length == 0)
				return GetCompletedAwaitable();

			AwaitableCompletionSource source = new AwaitableCompletionSource();
			int remaining = awaitables.Length;
			int failed = 0; // 0 = 아직 성공 상태, 1 = 이미 실패(예외) 전파됨

			for (int i = 0; i < awaitables.Length; i++) {
				_ = AwaitOne(awaitables[i]);
			}

			return source.Awaitable;

			async Awaitable AwaitOne(Awaitable a) {
				try {
					await a;
				}
				catch (Exception ex) {
					// 첫 번째 예외가 승리 → 즉시 실패 전파
					if (Interlocked.Exchange(ref failed, 1) == 0) {
						source.TrySetException(ex);
					}
				}
				finally {
					// 남은 개수 감소. 실패가 이미 전파됐다면 결과 세팅은 하지 않음.
					if (Interlocked.Decrement(ref remaining) == 0 && Volatile.Read(ref failed) == 0) {
						source.TrySetResult();
					}
				}
			}
		}

		public static Awaitable GetCompletedAwaitable() {
			AwaitableCompletionSource s = new AwaitableCompletionSource();
			s.SetResult();
			return s.Awaitable;
		}
	}
}