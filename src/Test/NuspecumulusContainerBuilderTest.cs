using Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Interfaces;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Test;

[TestFixture]
public class NuspecumulusContainerBuilderTest {
    [Test]
    public void NuspecumulusContainerBuilder_CanBuild() {
        var container = new ContainerBuilder().UseNuspecumulus().Build();
        var creator = container.Resolve<INuSpecCreator>();
        Assert.That(creator, Is.Not.Null);
    }
}