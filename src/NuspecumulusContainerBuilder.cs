using Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Components;
using Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Interfaces;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.Nuspecumulus;

public static class NuspecumulusContainerBuilder {
    public static ContainerBuilder UseNuspecumulus(this ContainerBuilder builder) {
        builder.RegisterType<NuSpecCreator>().As<INuSpecCreator>();
        return builder;
    }
}