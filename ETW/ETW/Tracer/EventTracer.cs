﻿using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using Rx.MainModule;

namespace ETW.Tracer
{
    /// <summary>
    /// Kernel event listener.
    /// </summary>
    class EventTracer
    {
        private readonly TextWriter _out;
        private TraceEventSession _kernelSession;
        private bool _isStopped;

        private BaseObservable<InternalEvent> dllInput;
        private BaseObservable<InternalEvent> fwInput;
        private BaseObservable<InternalEvent> frInput;

        public EventTracer()
        {
            _out = Console.Out;
        }

        public EventTracer(TextWriter @out)
        {
            _out = @out;
        }

        public void setInputs(BaseObservable<InternalEvent> pdllInput,
                              BaseObservable<InternalEvent> pfwInput,
                              BaseObservable<InternalEvent> pfrInput)
        {
            dllInput = pdllInput;
            fwInput = pfwInput;
            frInput = pfrInput;
        }

        /// <summary>
        /// Start event tracing.
        /// </summary>
        public void Run()
        {
            if (!(TraceEventSession.IsElevated() ?? false))
            {
                _out.WriteLine("Please run program as Administrator");
                Debugger.Break();
                return;
            }

            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs cancelArgs)
            {
                _isStopped = true;
                _out.WriteLine("Stopping all ETW sessions...");
                _kernelSession?.Dispose();
                cancelArgs.Cancel = true;
            };

            using (_kernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName))
            {

                _kernelSession.EnableKernelProvider(KernelTraceEventParser.Keywords.All);
                _kernelSession.Source.Kernel.ImageLoad += ImageLoadEvent;
                _kernelSession.Source.Kernel.FileIOWrite += FileWriteEvent;
                _kernelSession.Source.Kernel.FileIORead += FileReadEvent;

                _kernelSession.Source.Process();
            }
        }

        private void ImageLoadEvent(ImageLoadTraceData data)
        {
            if (_isStopped)
            {
                return;
            }

#if DEBUG
            _out.WriteLine("ImageLoadEvent from pid {0} caught", data.ProcessID);
#endif
            dllInput.AddEvent(new InternalEvent(data.ID, data.ProcessID, data.TimeStampRelativeMSec));
        }

        private void FileWriteEvent(FileIOReadWriteTraceData data)
        {
            if (_isStopped)
            {
                return;
            }

#if DEBUG
            _out.WriteLine("FileWriteEvent from pid {0} caught", data.ProcessID);
#endif
            fwInput.AddEvent(new InternalEvent(data.ID, data.ProcessID, data.TimeStampRelativeMSec));
        }

        private void FileReadEvent(FileIOReadWriteTraceData data)
        {
            // HasExtension(...) is to avoid cached files
            if (_isStopped || !Path.HasExtension(data.FileName))
            {
                return;
            }

#if DEBUG
            _out.WriteLine("FileReadEvent from pid {0} caught", data.ProcessID);
#endif
            frInput.AddEvent(new InternalEvent(data.ID, data.ProcessID, data.TimeStampRelativeMSec));
        }
    }
}
