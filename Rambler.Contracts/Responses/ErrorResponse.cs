namespace Rambler.Contracts.Responses
{
    [MessageKey("ERROR")]
    public class ErrorResponse
    {
        public enum ErrorCode
        {
            NotInChannel = 0,
            NotAuthenticated = 1,
            NickInUse = 2,
            InvalidName = 3,
            NoSuchChannel = 4,
            None = 5,
            AlreadyInChannel = 6,
            NoGuestsAllowed = 7,
        }

        public int Code { get; set; }
    }
}
