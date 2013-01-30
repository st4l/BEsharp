namespace BESharp
{
    public enum ShutdownReason
    {
        None,
        NoResponseFromServer,
        UserRequested,
        ServerRequested,
        FatalException
    }
}
