namespace Aspenlaub.Net.GitHub.CSharp.Nuspecumulus.Entities;

public class Version {
    public int Major { init; get; }
    public int Minor { init; get; }
    public int Build { get; set; }
    public int Revision { get; set; }

    public override string ToString() {
        return $"{Major}.{Minor}.{Build}.{Revision}";
    }
}