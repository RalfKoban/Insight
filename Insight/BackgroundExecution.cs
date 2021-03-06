﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
// ReSharper disable UnusedVariable

namespace Insight
{
    public sealed class BackgroundExecution
    {
        private readonly ProgressService _progressService;
        private readonly DialogService _dialogs;

        public BackgroundExecution(ProgressService progressService, DialogService dialogs)
        {
            _progressService = progressService;
            _dialogs = dialogs;
        }

        /// <summary>
        /// Call from Ui thread
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<T> func)
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == Application.Current.Dispatcher.Thread.ManagedThreadId);

            var result = default(T);
            Exception exception = null;
            using (var progress = _progressService.CreateProgress())
            {
                try
                {
                    result = await Task.Run(() => func()).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }

            if (exception != null)
            {
                _dialogs.ShowError(exception.Message);
            }

            return result;
        }

        /// <summary>
        /// Call from Ui thread
        /// </summary>
        public async Task ExecuteAsync(Action action)
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == Application.Current.Dispatcher.Thread.ManagedThreadId);

            Exception exception = null;
            using (var progress = _progressService.CreateProgress())
            {
                try
                {
                    await Task.Run(() => action()).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }

            if (exception != null)
            {
                _dialogs.ShowError(exception.Message);
            }
        }

        /// <summary>
        /// Call from Ui thread
        /// </summary>
        public async Task ExecuteWithProgressAsync(Action<Progress> action)
        {
            Debug.Assert(Thread.CurrentThread.ManagedThreadId == Application.Current.Dispatcher.Thread.ManagedThreadId);

            Exception exception = null;
            using (var progress = _progressService.CreateProgress())
            {
                try
                {
                    await Task.Run(() => action(progress)).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }

            if (exception != null)
            {
                _dialogs.ShowError(exception.Message);
            }
        }
    }
}