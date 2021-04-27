namespace Rambler.Contracts
{
    using System;

    /// <summary>
    /// Helper attribute for adding request types to the distributor
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class MessageKey : Attribute
    {
        public string Key { get; private set; }
        public MessageKey(string key)
        {
            Key = key;
        }
    }
}
