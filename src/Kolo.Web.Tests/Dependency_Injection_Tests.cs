﻿using System.Linq;
using System.Web.Http;
using Ninject;
using NUnit.Framework;

namespace Kolo.Web.Tests
{
    [TestFixture]
    class Dependency_Injection_Tests
    {
        [Test]
        public void Ninject_Test()
        {
            var kernel = NinjectWebCommon.CreateKernel();

            var appAssembly = typeof(NinjectWebCommon).Assembly;

            var controllerTypes =
                from type in appAssembly.GetExportedTypes()
                where typeof(ApiController).IsAssignableFrom(type)
                where !type.IsAbstract
                where !type.IsGenericTypeDefinition
                where type.Name.EndsWith("Controller")
                select type;

            foreach (var controllerType in controllerTypes)
                kernel.Get(controllerType);
        }
    }
}
