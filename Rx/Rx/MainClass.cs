﻿using Microsoft.Diagnostics.Tracing;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Rx.MainModule;
using Rx.Observers;
using System.Reactive.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Rx
{
    public class MainClass
    {
        public static void Main(string[] args) 
        {
            ConnectionTest();
        }



        public static void ConnectionTest() 
        {
           /* List<AutoResetEvent> events = new List<AutoResetEvent>(2);
            for (int i = 0; i < 3; i++)
            {
                events.Add(new AutoResetEvent(false));
            }*/
            //var input = new BaseObservable<InternalEvent>();
            
            //input.ObserveOn(TaskPoolScheduler.Default).Subscribe(BaseObserver<InternalEvent,InternalEvent>.OnNext,ex=>Console.WriteLine(ex.Message),BaseObserver<InternalEvent, InternalEvent>.OnCompleted);
            /*first.ConnectTo(second);
            second.ConnectTo(killer);*/
            //input.Stop();
            //AutoResetEvent.WaitAll(events.ToArray());
            IObservable<InternalEvent> ticketObservable = System.Reactive.Linq.Observable.Create<InternalEvent>(EventFactory.EventSubscribe).Where(elem => elem.ProcessID % 2 == 0);
            using (IDisposable handle = ticketObservable.Subscribe(BaseObserver<InternalEvent, InternalEvent>.OnNext, ex => Console.WriteLine(ex.Message), BaseObserver<InternalEvent, InternalEvent>.OnCompleted))
            {
                Console.WriteLine("\nPress ENTER to unsubscribe...\n");
                Console.ReadLine();
            }

        }


    }
    public class EventFactory : IDisposable
    {
        private bool bGenerate = true;


        internal EventFactory(object eventObserver)
        {
            //************************************************************************//
            //*** The sequence generator for tickets will be run on another thread ***//
            //************************************************************************//
            Task.Factory.StartNew(new Action<object>(EventGenerator), eventObserver);
        }


        //**************************************************************************************************//
        //*** Dispose frees the ticket generating resources by allowing the TicketGenerator to complete. ***//
        //**************************************************************************************************//
        public void Dispose()
        {
            bGenerate = false;
        }


        //*****************************************************************************************************************//
        //*** TicketGenerator generates a new ticket every 3 sec and calls the observer's OnNext handler to deliver it. ***//
        //*****************************************************************************************************************//
        private void EventGenerator(object observer)
        {
            IObserver<InternalEvent> ticketObserver = (IObserver<InternalEvent>)observer;

            //***********************************************************************************************//
            //*** Generate a new ticket every 3 sec and call the observer's OnNext handler to deliver it. ***//
            //***********************************************************************************************//
            InternalEvent t;
            int i = 0;
            while (bGenerate)
            {
                t = new InternalEvent(new TraceEventID(),"hoh",i,i);
                ticketObserver.OnNext(t);
                Thread.Sleep(1000);
                i++;
            }
        }



        //********************************************************************************************************************************//
        //*** TicketSubscribe starts the flow of tickets for the ticket sequence when a subscription is created. It is passed to       ***//
        //*** Observable.Create() as the subscribe parameter. Observable.Create() returns the IObservable<Ticket> that is used to      ***//
        //*** create subscriptions by calling the IObservable<Ticket>.Subscribe() method.                                              ***//
        //***                                                                                                                          ***//
        //*** The IDisposable interface returned by TicketSubscribe is returned from the IObservable<Ticket>.Subscribe() call. Calling ***//
        //*** Dispose cancels the subscription freeing ticket generating resources.                                                    ***//
        //********************************************************************************************************************************//
        public static IDisposable EventSubscribe(object ticketObserver)
        {
            EventFactory tf = new EventFactory(ticketObserver);

            return tf;
        }
    }

}
