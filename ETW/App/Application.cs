﻿using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using App.IoC;
using ETW.Provider;
using ETW.Reflection;
using Kit;
using ETW.Tracer;
using Unity;

namespace App
{
    public class Application
    {
        private static readonly Subject<SuspiciousEvent> SuspiciousEvents = new Subject<SuspiciousEvent>();
        private static readonly EventTracer EventTracer = new EventTracer();
        private static readonly Dashboard Dashboard = new Dashboard();
        private static readonly IUnityContainer Container = new UnityContainer();

        /// <summary>
        /// Run application.
        /// </summary>
        /// <param name="args">Rx DLLs</param>
        public static void Run(string[] args)
        {
            var task = Task.Run(EventTracer.Test);
            Thread.Sleep(1000);
            SuspiciousEvents.Subscribe(ev =>
            {
                Dashboard.AddOrUpdate(ev.ProcessId, new KeyValuePair<SuspiciousEvent, int>(ev,1));
                Dashboard.Show();
            });
            ContainerConfigurator.Initialization(Container, SuspiciousEvents);

            foreach (var value in args)
            {
                LoadAnalyzer(value);
            }

            Console.WriteLine("Please provide path to dll. Enter 'load `path to dll`'");
            var line = Console.ReadLine();
            while (!line.Equals("exit"))
            {
                if (int.TryParse(line, out var id))
                {
                    Dashboard.Kill(id);
                }
                else if (line.Contains("load"))
                {
                    //line = line.Replace("load", "").Replace(" ", "");

                    LoadAnalyzer(line.Remove(0,5));
                    line = Console.ReadLine();
                }
                else 
                {
                    Console.WriteLine("Enter valid id");
                    line = Console.ReadLine();
                }
            }
            Console.WriteLine("Stopping application...");
            EventTracer.GetKernelSession()?.Dispose();
            Console.WriteLine("Application stopped\nPress any key to exit");
            Console.ReadKey();
        }

        public static void LoadAnalyzer(string value)
        {
            var assembly = Assembly.LoadFile(value);
            var type = TypeFinder.GetType(assembly);
            var arguments = ReflectionKit.GetConstructorArgs(type, EventTracer);
            foreach (var provider in arguments)
            {
                var iProvider = provider as IEventProvider;
                iProvider?.Subscribe(Container);
            }
            var analyzer = (ARxAnalyzer)Container.Resolve(type);
            Task.Run(analyzer.Start);
        }
    }
}