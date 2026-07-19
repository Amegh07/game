namespace MuseumHeist.Cyber
{
    public interface ITerminalAction
    {
        string ActionName { get; }
        string Description { get; }
        string RequiredPermission { get; }
        bool Execute(TerminalActionContext context, out string resultMessage);
    }

    public class TerminalActionContext
    {
        public string TargetID { get; set; }
        public string[] TargetIDs { get; set; }
        public NetworkSession Session { get; set; }
    }
}
