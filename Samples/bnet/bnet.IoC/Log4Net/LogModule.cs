// ----------------------------------------------------------------------------------------------------
// <copyright file="LogModule.cs" company="Me">Copyright (c) 2012 St4l.</copyright>
// ----------------------------------------------------------------------------------------------------
namespace BNet.IoC.Log4Net
{
    using System;
    using System.Linq;
    using Autofac;
    using Autofac.Core;


    public abstract class LogModule<TLogger> : Module
    {
        protected override void AttachToComponentRegistration(
            IComponentRegistry componentRegistry, IComponentRegistration registration)
        {
            var type = registration.Activator.LimitType;
            if (this.HasPropertyDependencyOnLogger(type))
            {
                registration.Activated += this.InjectLoggerViaProperty;
            }

            if (this.HasConstructorDependencyOnLogger(type))
            {
                registration.Preparing += this.InjectLoggerViaConstructor;
            }
        }


        protected abstract TLogger CreateLoggerFor(Type type);


        private bool HasConstructorDependencyOnLogger(Type type)
        {
            return
                type.GetConstructors()
                    .SelectMany(
                        ctor =>
                        ctor.GetParameters()
                            .Where(parameter => parameter.ParameterType == typeof(TLogger)))
                    .Any();
        }


        private bool HasPropertyDependencyOnLogger(Type type)
        {
            return
                type.GetProperties()
                    .Any(property => property.CanWrite && property.PropertyType == typeof(TLogger));
        }


        private void InjectLoggerViaConstructor(object sender, PreparingEventArgs @event)
        {
            var type = @event.Component.Activator.LimitType;
            @event.Parameters =
                @event.Parameters.Union(
                    new[]
                        {
                            new ResolvedParameter(
                                (parameter, context) => parameter.ParameterType == typeof(TLogger), 
                                (p, i) => this.CreateLoggerFor(type))
                        });
        }


        private void InjectLoggerViaProperty(object sender, ActivatedEventArgs<object> @event)
        {
            var type = @event.Instance.GetType();
            var propertyInfo =
                type.GetProperties().First(x => x.CanWrite && x.PropertyType == typeof(TLogger));
            propertyInfo.SetValue(@event.Instance, this.CreateLoggerFor(type), null);
        }
    }
}
