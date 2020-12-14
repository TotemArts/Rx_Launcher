public static class RenXWebLinks
{
    public static readonly char RenxServerSettingSpaceSymbol = ';';
    /**
     * Data contains basic details about the active servers.
     */
    private const string RenxServerJsonUrl = "https://serverlist.renegade-x.com/servers.jsp";
    /**
     * Data contains advanced details about the active servers.
     * Same as 'RenxServerJsonUrl' with addition data of Players and playable Levels.
     */
    private const string RenxServerLongJsonUrl = "https://serverlist.renegade-x.com/servers_long.jsp";
    public const string RenxActiveServerJsonUrl = RenxServerLongJsonUrl;
}