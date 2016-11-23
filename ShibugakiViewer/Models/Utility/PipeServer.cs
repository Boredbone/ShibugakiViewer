using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Boredbone.Utility.Notification;
using Reactive.Bindings.Extensions;

namespace ShibugakiViewer.Models.Utility
{

    public class PipeServer : DisposableBase
    {

        private Subject<string> LineReceivedSubject { get; }
        public IObservable<string> LineReceived => this.LineReceivedSubject.AsObservable();

        public PipeServer()
        {
            this.LineReceivedSubject = new Subject<string>().AddTo(this.Disposables);
        }

        public void Activate(string mutexId, string pipeId)
        {

            var tokenSource = new CancellationTokenSource();
            var t = RunAsync(mutexId, pipeId, tokenSource).ContinueWith(y =>
            {
                if (y.Exception != null && this.LineReceivedSubject.HasObservers)
                {
                    this.LineReceivedSubject.OnError(y.Exception);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);

            Disposable.Create(() =>
            {
                tokenSource.Cancel();


                using (var pipeClient =
                    new NamedPipeClientStream(".", pipeId, PipeDirection.Out))
                {
                    pipeClient.Connect(100);
                }
            }).AddTo(this.Disposables);
        }

        private Task RunAsync(string mutexId, string pipeId,
            CancellationTokenSource cancellationTokenSource)
        {
            var cancellationToken = cancellationTokenSource.Token;
            return Task.Run(async () =>
            {
                try
                {
                    using (var mutex = new Mutex(false, mutexId))
                    {
                        //ミューテックスの所有権を要求する
                        if (!mutex.WaitOne(0, false))
                        {
                            this.LineReceivedSubject.OnError(new Exception("Already launched"));
                            //すでに起動していると判断して終了
                            //Mutexを取得できなかったので解放の必要はない
                            return;
                        }


                        try
                        {
                            //mutexDictionary.TryAdd(mutexId, mutex);

                            while (true)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                using (var pipeServer =
                                    new NamedPipeServerStream(pipeId, PipeDirection.In))
                                {
                                    // Wait for a client to connect
                                    await pipeServer.WaitForConnectionAsync(cancellationToken);
                                    //pipeServer.WaitForConnection();
                                    cancellationToken.ThrowIfCancellationRequested();

                                    try
                                    {
                                        using (var sr = new StreamReader(pipeServer))
                                        {
                                            while (pipeServer.IsConnected)
                                            {
                                                cancellationToken.ThrowIfCancellationRequested();

                                                var text = sr.ReadLine();
                                                if (text != null)
                                                {
                                                    this.LineReceivedSubject.OnNext(text);
                                                }
                                            }
                                        }
                                    }
                                    catch (IOException)
                                    {
                                        // Catch the IOException that is raised if the pipe is broken
                                        // or disconnected.
                                    }
                                }
                            }
                        }
                        finally
                        {
                            mutex.ReleaseMutex();
                            mutex.Close();
                        }
                    }
                }
                catch (Exception e)
                {
                    if (this.LineReceivedSubject.HasObservers)
                    {
                        this.LineReceivedSubject.OnError(e);
                    }
                }
            }, cancellationToken);
        }
    }
}
