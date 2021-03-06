﻿using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Bebbs.Harmonize.With.Harmony.Messages;

namespace Bebbs.Harmonize.With.Harmony.State
{
    public interface IMachine : IInitialize, IStart, IStop, ICleanup
    {

    }

    public class Machine : IMachine
    {
        private readonly IFactory _stateFactory;
        private readonly Settings.IProvider _settingsProvider;
        private readonly Messages.IMediator _messageMediator;
        private readonly IAsyncHelper _asyncHelper;

        private IDisposable _transitionSubscription;
        private IState _currentState;
        private IContext _currentContext;

        public Machine(IFactory stateFactory, Settings.IProvider settingsProvider, Messages.IMediator messageMediator, IAsyncHelper asyncHelper)
        {
            _stateFactory = stateFactory;
            _settingsProvider = settingsProvider;
            _messageMediator = messageMediator;
            _asyncHelper = asyncHelper;
        }

        private void EnterState<TContext>(IState<TContext> state, TContext context)
            where TContext : IContext
        {
            state.OnEnter(context);
        }

        private void EnterState()
        {
            dynamic currentState = _currentState;
            dynamic currentContext = _currentContext;

            EnterState(currentState, currentContext);
        }

        private void ExitState<TContext>(IState<TContext> state, TContext context)
            where TContext : IContext
        {
            state.OnExit(context);
        }

        private void ExitState()
        {
            dynamic currentState = _currentState;
            dynamic currentContext = _currentContext;

            ExitState(currentState, currentContext);
        }

        private void ChangeIntoState<TContext>(ITransitionToStateMessage<TContext> message)
            where TContext : IContext
        {
            _currentState = _stateFactory.ConstructState(message.State);
            _currentContext = message.Context;
        }

        private void ChangeState(ITransitionToStateMessage message)
        {
            dynamic stateMessage = message;

            ChangeIntoState(stateMessage);
        }

        private void ProcessTransition(ITransitionToStateMessage message)
        {
            ExitState();

            ChangeState(message);

            EnterState();
        }

        public void Initialize()
        {
            Settings.IValues settings = _settingsProvider.GetValues();

            _currentState = _stateFactory.ConstructState(Name.Stopped);
            _currentContext = ContextFactory.Create(settings.EMail, settings.Password);

            EnterState();

            _transitionSubscription = _messageMediator.GetEvent<ITransitionToStateMessage>().ObserveOn(_asyncHelper.AsyncScheduler).Subscribe(ProcessTransition);
        }

        public Task Start()
        {
            IObservable<Unit> observable = ObservableExtentions.Either(
                _messageMediator.GetEvent<Messages.IStartedMessage>().Timeout(TimeSpan.FromSeconds(30)),
                _messageMediator.GetEvent<Messages.IErrorMessage>(),
                (started, error) =>
                {
                    if (error != null)
                    {
                        throw new ApplicationException("Error starting Harmony", error.Exception);
                    }
                    else
                    {
                        return Unit.Default;
                    }
                }
            );

            Task result = observable.Take(1).ToTask();

            _messageMediator.Publish(new Messages.StartHarmonizingMessage());

            return result;
        }

        public Task Stop()
        {
            if (_currentState is State.StoppableState)
            {
                IObservable<Unit> observable = ObservableExtentions.Either(
                    _messageMediator.GetEvent<Messages.IStoppedMessage>().Timeout(TimeSpan.FromSeconds(10)),
                    _messageMediator.GetEvent<Messages.IErrorMessage>(),
                    (started, error) =>
                    {
                        if (error != null)
                        {
                            throw new ApplicationException("Error stopping Harmony", error.Exception);
                        }
                        else
                        {
                            return Unit.Default;
                        }
                    }
                );


                Task result = observable.ToTask();

                _messageMediator.Publish(new Messages.StopHarmonizingMessage());

                return result;
            }
            else
            {
                return Task.FromResult<object>(null);
            }
        }

        public void Cleanup()
        {
            if (_transitionSubscription != null)
            {
                _transitionSubscription.Dispose();
                _transitionSubscription = null;
            }

            ExitState();
        }
    }
}
