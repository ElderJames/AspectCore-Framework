﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using AspectCore.DynamicProxy;
using AspectCore.Extensions.Reflection;
using Castle.DynamicProxy;
using CastleIntercrptor = Castle.DynamicProxy.IInterceptor;

namespace AspectCore.Extensions.Windsor
{
    public class AspectCoreInterceptor : CastleIntercrptor
    {
        private readonly IAspectBuilderFactory _aspectBuilderFactory;
        private readonly IAspectContextFactory _aspectContextFactory;
        private readonly IAspectValidator _aspectValidator;

        public AspectCoreInterceptor(IAspectBuilderFactory aspectBuilderFactory,
            IAspectContextFactory aspectContextFactory, IAspectValidatorBuilder aspectValidatorBuilder)
        {
            _aspectBuilderFactory = aspectBuilderFactory ?? throw new ArgumentNullException(nameof(aspectBuilderFactory));
            _aspectContextFactory = aspectContextFactory ?? throw new ArgumentNullException(nameof(aspectContextFactory));
            _aspectValidator = aspectValidatorBuilder?.Build() ?? throw new ArgumentNullException(nameof(aspectValidatorBuilder));
        }

        public void Intercept(IInvocation invocation)
        {
            if (!_aspectValidator.Validate(invocation.Method))
            {
                invocation.Proceed();
                return;
            }
            if (invocation.Proxy == null)
            {
                return;
            }
            var proxyTypeInfo = invocation.Proxy.GetType().GetTypeInfo();
            var builderFactory = new CastleAspectBuilderFactory(_aspectBuilderFactory, ctx =>
            {
                invocation.Proceed();
                return Task.FromResult(0);
            });
            var proxyMethod = proxyTypeInfo.GetMethod(new MethodSignature(invocation.Method)) ?? proxyTypeInfo.GetExplicitMethod(invocation.Method);
            var activator = new AspectActivatorFactory(_aspectContextFactory, builderFactory).Create();
            var activatorContext = new AspectActivatorContext(invocation.Method, invocation.MethodInvocationTarget, proxyMethod, invocation.InvocationTarget, invocation.Proxy, invocation.Arguments);
            var reflector = InterceptUtils.GetInvokeReflector(invocation.Method);
            invocation.ReturnValue = reflector.Invoke(activator, activatorContext);
        }
    }
}