namespace BESharp
{
    public class CommandResult
    {
        internal CommandResult(bool acknowledged, string body)
        {
            this.Succeeded = acknowledged;
            this.Response = body;
        }


        public bool Succeeded { get; private set; }


        public string Response { get; private set; }
    }
}