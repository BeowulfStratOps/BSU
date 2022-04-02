namespace BSU.Server;

public class ModUpdateStats
{
    public readonly string ModName;
    public int Deleted, Updated, New;

    public ModUpdateStats(string modName)
    {
        ModName = modName;
    }

}
