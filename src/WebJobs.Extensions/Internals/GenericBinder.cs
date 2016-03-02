﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Microsoft.Azure.WebJobs.ServiceBus
{
    // Helpers for doing general-purpose bindings. 
    internal class GenericBinder
    {
        public static ITriggerBinding GetTriggerBinding<TMessage, TTriggerValue>(
            ITriggerBindingStrategy<TMessage, TTriggerValue> hooks,
            ParameterInfo parameter,
            IConverterManager converterManager,
            Func<ListenerFactoryContext, bool, Task<IListener>> createListener)
        {
            bool singleDispatch;
            var argumentBinding = GenericBinder.GetTriggerArgumentBinding(hooks, parameter, converterManager, out singleDispatch);

            var parameterDescriptor = new ParameterDescriptor
            {
                Name = parameter.Name,
                DisplayHints = new ParameterDisplayHints
                {
                    Description = singleDispatch ? "message" : "messages"
                }
            };

            ITriggerBinding binding = new CommonTriggerBinding<TMessage, TTriggerValue>(
                hooks, argumentBinding, createListener, parameterDescriptor, singleDispatch);

            return binding;
        }

        // Bind a trigger argument to various parameter types. 
        // Handles either T or T[], 
        public static ITriggerDataArgumentBinding<TTriggerValue> GetTriggerArgumentBinding<TMessage, TTriggerValue>(
            ITriggerBindingStrategy<TMessage, TTriggerValue> hooks, 
            ParameterInfo parameter, 
            IConverterManager converterManager,
            out bool singleDispatch)
        {
            ITriggerDataArgumentBinding<TTriggerValue> argumentBinding = null;
            if (parameter.ParameterType.IsArray)
            {
                // dispatch the entire batch in a single call. 
                singleDispatch = false;

                var elementType = parameter.ParameterType.GetElementType();

                var innerArgumentBinding = GetTriggerArgumentElementBinding<TMessage, TTriggerValue>(elementType, hooks, converterManager);

                argumentBinding = new ArrayTriggerArgumentBinding<TMessage, TTriggerValue>(hooks, innerArgumentBinding, converterManager);

                return argumentBinding;
            }
            else
            {
                // Dispatch each item one at a time
                singleDispatch = true;

                var elementType = parameter.ParameterType;
                argumentBinding = GetTriggerArgumentElementBinding<TMessage, TTriggerValue>(elementType, hooks, converterManager);
                return argumentBinding;
            }
        }
        
        // Bind a T. 
        private static SimpleTriggerArgumentBinding<TMessage, TTriggerValue> GetTriggerArgumentElementBinding<TMessage, TTriggerValue>(
            Type elementType, 
            ITriggerBindingStrategy<TMessage, TTriggerValue> hooks,
            IConverterManager converterManager)
        {
            if (elementType == typeof(TMessage))
            {
                return new SimpleTriggerArgumentBinding<TMessage, TTriggerValue>(hooks, converterManager);
            }
            if (elementType == typeof(string))
            {
                return new StringTriggerArgumentBinding<TMessage, TTriggerValue>(hooks, converterManager);
            }
            else
            {
                // Default, assume a Poco
                return new PocoTriggerArgumentBinding<TMessage, TTriggerValue>(hooks, converterManager, elementType);
            }         
        }

        /// <summary>
        /// Creates a binding that binds to an <see cref="IFlushCollector{T}"/> with the specified argument type. Allows for a binding
        /// to be generated for any POCO type.
        /// </summary>
        /// <param name="parameter">The ParameterInfo for the binding</param>
        /// <param name="collectorGenericType">The generic type that must implement <see cref="IFlushCollector{T}"/> and have a public constructor with at most one parameter.</param>
        /// <param name="collectorGenericArgumentType">The generic argument type for the <see cref="IFlushCollector{T}"/></param>
        /// <param name="converterManager">A converter manager to pass along.</param>        
        /// <param name="invokeStringBinder">A <see cref="Func{T, TContext}"/> that returns the TContext to be used by the <see cref="IFlushCollector{T}"/></param>
        /// <returns></returns>
        public static IBinding BindGenericCollector<TContext>(ParameterInfo parameter, Type collectorGenericType, Type collectorGenericArgumentType,
            IConverterManager converterManager, Func<string, TContext> invokeStringBinder)
        {
            if (!collectorGenericType.IsGenericTypeDefinition)
            {
                throw new ArgumentException("The parameter 'collectorGenericType' must be a generic type definition.");
            }
            if (collectorGenericType.GetInterface(typeof(IFlushCollector<>).Name) == null)
            {
                throw new ArgumentException("The Type specified by parameter 'collectorGenericType' must implement IFlushCollector<T>.");
            }

            Type flushCollectorInterfaceType = typeof(IFlushCollector<>).MakeGenericType(collectorGenericArgumentType);
            Type actualCollectorType = collectorGenericType.MakeGenericType(collectorGenericArgumentType);

            // Create a delegate to pass as the builder func to BindCollector
            Type funcType = typeof(Func<,,>).MakeGenericType(typeof(object), typeof(ValueBindingContext), flushCollectorInterfaceType);
            MethodInfo getCollector = typeof(GenericBinder).GetMethod("GetCollector", BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(actualCollectorType, collectorGenericArgumentType);
            var del = Delegate.CreateDelegate(funcType, getCollector);           

            // Create the method and parameters.
            MethodInfo bindCollectorMethod = typeof(GenericBinder).GetMethod("BindCollector").MakeGenericMethod(collectorGenericArgumentType, typeof(TContext));
            object[] parameters = new object[] { parameter, converterManager, del, null, invokeStringBinder };

            return bindCollectorMethod.Invoke(null, parameters) as IBinding;
        }   

        /// <summary>
        /// A method that is used as a instantiated as a <see cref="Func{T1, T2, TResult}"/> to pass to BindCollector
        /// </summary>
        /// <typeparam name="TCollector">The type of collector to create for the binding.</typeparam>
        /// <typeparam name="TCore">The 'core' type of the binding.</typeparam>
        /// <param name="userContext">The object to pass to the constructor of TCollector.</param>
        /// <param name="context">The ValueBindingContext (unused).</param>
        /// <returns>An <see cref="IFlushCollector{T}"/></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "context")]
        private static IFlushCollector<TCore> GetCollector<TCollector, TCore>(object userContext, ValueBindingContext context)
        {
            return Activator.CreateInstance(typeof(TCollector), userContext) as IFlushCollector<TCore>;
        }

        // Bind an IAsyncCollector<TMessage> to a user parameter. 
        // This handles the various flavors of parameter types and will morph them to the connector.
        // parameter  - parameter being bound. 
        // TContext - helper object to pass to the binding the configuration state. This can point back to context like secrets, configuration, etc.
        // builder - function to create a new instance of the underlying Collector object to pass to the parameter. 
        //          This binder will wrap that in any adpaters to make it match the requested parameter type.
        public static IBinding BindCollector<TMessage, TContext>(
            ParameterInfo parameter,
            IConverterManager converterManager,
            Func<TContext, ValueBindingContext, IFlushCollector<TMessage>> builder,
            string invokeString,
            Func<string, TContext> invokeStringBinder)
        {
            Type parameterType = parameter.ParameterType;

            Func<TContext, ValueBindingContext, IValueProvider> argumentBuilder = null;            

            if (parameterType.IsGenericType)
            {
                var genericType = parameterType.GetGenericTypeDefinition();
                var elementType = parameterType.GetGenericArguments()[0];

                if (genericType == typeof(IAsyncCollector<>))
                {
                    if (elementType == typeof(TMessage))
                    {
                        // Bind to IAsyncCollector<TMessage>. This is the "purest" binding, no adaption needed. 
                        argumentBuilder = (context, valueBindingContext) =>
                        {
                            IFlushCollector<TMessage> raw = builder(context, valueBindingContext);
                            return new CommonAsyncCollectorValueProvider<IAsyncCollector<TMessage>, TMessage>(raw, raw, invokeString);
                        };
                    }
                    else
                    {
                        // Bind to IAsyncCollector<T>
                        // Get a converter from T to TMessage
                        argumentBuilder = DynamicInvokeBuildIAsyncCollectorArgument(elementType, converterManager, builder, invokeString);
                    }
                }
                else if (genericType == typeof(ICollector<>))
                {
                    if (elementType == typeof(TMessage))
                    {
                        // Bind to ICollector<TMessage> This just needs an Sync/Async wrapper
                        argumentBuilder = (context, valueBindingContext) =>
                        {
                            IFlushCollector<TMessage> raw = builder(context, valueBindingContext);
                            ICollector<TMessage> obj = new SyncAsyncCollectorAdapter<TMessage>(raw);
                            return new CommonAsyncCollectorValueProvider<ICollector<TMessage>, TMessage>(obj, raw, invokeString);
                        };
                    }
                    else
                    {
                        // Bind to ICollector<T>. 
                        // This needs both a conversion from T to TMessage and an Sync/Async wrapper
                        argumentBuilder = DynamicInvokeBuildICollectorArgument(elementType, converterManager, builder, invokeString);
                    }
                }
            }

            if (parameter.IsOut)
            {
                Type elementType = parameter.ParameterType.GetElementType();

                if (elementType.IsArray)
                {
                    if (elementType == typeof(TMessage[]))
                    {
                        argumentBuilder = (context, valueBindingContext) =>
                        {
                            IFlushCollector<TMessage> raw = builder(context, valueBindingContext);
                            return new OutArrayValueProvider<TMessage>(raw, invokeString);
                        };
                    }
                    else
                    {
                        // out TMessage[]
                        var e2 = elementType.GetElementType();
                        argumentBuilder = DynamicBuildOutArrayArgument(e2, converterManager, builder, invokeString);
                    }
                }
                else
                {
                    // Single enqueue
                    //    out TMessage
                    if (elementType == typeof(TMessage))
                    {
                        argumentBuilder = (context, valueBindingContext) =>
                        {
                            IFlushCollector<TMessage> raw = builder(context, valueBindingContext);
                            return new OutValueProvider<TMessage>(raw, invokeString);
                        };
                    }
                    else
                    {
                        // use JSon converter
                        // out T
                        argumentBuilder = DynamicInvokeBuildOutArgument(elementType, converterManager, builder, invokeString);
                    }
                }
            }

            if (argumentBuilder != null)
            {
                ParameterDescriptor param = new ParameterDescriptor
                {
                    Name = parameter.Name,
                    DisplayHints = new ParameterDisplayHints
                    {
                        Description = "output"
                    }
                };

                var initialClient = invokeStringBinder(invokeString);
                return new CommonCollectorBinding<TMessage, TContext>(initialClient, argumentBuilder, param, invokeStringBinder);
            }

            string msg = string.Format(CultureInfo.CurrentCulture, "Can't bind to {0}.", parameter);
            throw new InvalidOperationException(msg);
        }

        // Helper to dynamically invoke BuildICollectorArgument with the proper generics
        private static Func<TContext, ValueBindingContext, IValueProvider> DynamicBuildOutArrayArgument<TContext, TMessage>(
                Type typeMessageSrc,
                IConverterManager cm,
                Func<TContext, ValueBindingContext, IFlushCollector<TMessage>> builder,
                string invokeString)
        {
            var method = typeof(GenericBinder).GetMethod("BuildOutArrayArgument", BindingFlags.NonPublic | BindingFlags.Static);
            method = method.MakeGenericMethod(typeof(TContext), typeMessageSrc, typeof(TMessage));
            var argumentBuilder = (Func<TContext, ValueBindingContext, IValueProvider>)
            method.Invoke(null, new object[] { cm, builder, invokeString });
            return argumentBuilder;
        }

        private static Func<TContext, ValueBindingContext, IValueProvider> BuildOutArrayArgument<TContext, TMessageSrc, TMessage>(
            IConverterManager cm,
            Func<TContext, ValueBindingContext, IFlushCollector<TMessage>> builder,
            string invokeString)
        {
            // Other 
            Func<TMessageSrc, TMessage> convert = cm.GetConverter<TMessageSrc, TMessage>();
            Func<TContext, ValueBindingContext, IValueProvider> argumentBuilder = (context, valueBindingContext) =>
            {
                IFlushCollector<TMessage> raw = builder(context, valueBindingContext);
                IFlushCollector<TMessageSrc> obj = new TypedAsyncCollectorAdapter<TMessageSrc, TMessage>(raw, convert);

                return new OutArrayValueProvider<TMessageSrc>(obj, invokeString);
            };
            return argumentBuilder;
        }
        
        // Helper to dynamically invoke BuildICollectorArgument with the proper generics
        private static Func<TContext, ValueBindingContext, IValueProvider> DynamicInvokeBuildOutArgument<TContext, TMessage>(
                Type typeMessageSrc,
                IConverterManager cm,
                Func<TContext, ValueBindingContext, IFlushCollector<TMessage>> builder,
                string invokeString)
        {
            var method = typeof(GenericBinder).GetMethod("BuildOutArgument", BindingFlags.NonPublic | BindingFlags.Static);
            method = method.MakeGenericMethod(typeof(TContext), typeMessageSrc, typeof(TMessage));
            var argumentBuilder = (Func<TContext, ValueBindingContext, IValueProvider>)
            method.Invoke(null, new object[] { cm, builder, invokeString });
            return argumentBuilder;
        }

        private static Func<TContext, ValueBindingContext, IValueProvider> BuildOutArgument<TContext, TMessageSrc, TMessage>(
            IConverterManager cm,
            Func<TContext, ValueBindingContext, IFlushCollector<TMessage>> builder,
            string invokeString)
        {
            // Other 
            Func<TMessageSrc, TMessage> convert = cm.GetConverter<TMessageSrc, TMessage>();
            Func<TContext, ValueBindingContext, IValueProvider> argumentBuilder = (context, valueBindingContext) =>
            {
                IFlushCollector<TMessage> raw = builder(context, valueBindingContext);
                IFlushCollector<TMessageSrc> obj = new TypedAsyncCollectorAdapter<TMessageSrc, TMessage>(raw, convert);
                return new OutValueProvider<TMessageSrc>(obj, invokeString);
            };
            return argumentBuilder;
        }

        // Helper to dynamically invoke BuildICollectorArgument with the proper generics
        private static Func<TContext, ValueBindingContext, IValueProvider> DynamicInvokeBuildICollectorArgument<TContext, TMessage>(
                Type typeMessageSrc,
                IConverterManager cm,
                Func<TContext, ValueBindingContext, IFlushCollector<TMessage>> builder, 
                string invokeString)
        {
            var method = typeof(GenericBinder).GetMethod("BuildICollectorArgument", BindingFlags.NonPublic | BindingFlags.Static);
            method = method.MakeGenericMethod(typeof(TContext), typeMessageSrc, typeof(TMessage));
            var argumentBuilder = (Func<TContext, ValueBindingContext, IValueProvider>)
            method.Invoke(null, new object[] { cm, builder, invokeString });
            return argumentBuilder;
        }

        private static Func<TContext, ValueBindingContext, IValueProvider> BuildICollectorArgument<TContext, TMessageSrc, TMessage>(
            IConverterManager cm,
            Func<TContext, ValueBindingContext, IFlushCollector<TMessage>> builder,
            string invokeString)
        {
            // Other 
            Func<TMessageSrc, TMessage> convert = cm.GetConverter<TMessageSrc, TMessage>();
            Func<TContext, ValueBindingContext, IValueProvider> argumentBuilder = (context, valueBindingContext) =>
            {
                IFlushCollector<TMessage> raw = builder(context, valueBindingContext);
                IAsyncCollector<TMessageSrc> obj = new TypedAsyncCollectorAdapter<TMessageSrc, TMessage>(raw, convert);
                ICollector<TMessageSrc> obj2 = new SyncAsyncCollectorAdapter<TMessageSrc>(obj);
                return new CommonAsyncCollectorValueProvider<ICollector<TMessageSrc>, TMessage>(obj2, raw, invokeString);
            };
            return argumentBuilder;
        }

        // Helper to dynamically invoke BuildIAsyncCollectorArgument with the proper generics
        private static Func<TContext, ValueBindingContext, IValueProvider> DynamicInvokeBuildIAsyncCollectorArgument<TContext, TMessage>(
                Type typeMessageSrc,
                IConverterManager cm,
                Func<TContext, ValueBindingContext, IFlushCollector<TMessage>> builder,
                string invokeString)
        {
            var method = typeof(GenericBinder).GetMethod("BuildIAsyncCollectorArgument", BindingFlags.NonPublic | BindingFlags.Static);
            method = method.MakeGenericMethod(typeof(TContext), typeMessageSrc, typeof(TMessage));
            var argumentBuilder = (Func<TContext, ValueBindingContext, IValueProvider>)
            method.Invoke(null, new object[] { cm, builder, invokeString });
            return argumentBuilder;
        }

        // Helper to build an argument binder for IAsyncCollector<TMessageSrc>
        private static Func<TContext, ValueBindingContext, IValueProvider> BuildIAsyncCollectorArgument<TContext, TMessageSrc, TMessage>(
            IConverterManager cm,
            Func<TContext, ValueBindingContext, IFlushCollector<TMessage>> builder,
            string invokeString)
        {
            Func<TMessageSrc, TMessage> convert = cm.GetConverter<TMessageSrc, TMessage>();
            Func<TContext, ValueBindingContext, IValueProvider> argumentBuilder = (context, valueBindingContext) =>
            {
                IFlushCollector<TMessage> raw = builder(context, valueBindingContext);
                IAsyncCollector<TMessageSrc> obj = new TypedAsyncCollectorAdapter<TMessageSrc, TMessage>(raw, convert);
                return new CommonAsyncCollectorValueProvider<IAsyncCollector<TMessageSrc>, TMessage>(obj, raw, invokeString);
            };
            return argumentBuilder;
        }    
    }
}