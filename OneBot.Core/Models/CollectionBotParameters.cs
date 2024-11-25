namespace OneBot.Models
{
    public record class CollectionBotParameters
    {
        private readonly Dictionary<string, object> _otherParameters = [];

        public object this[string key]
        {
            get => _otherParameters[key];
            set => _otherParameters[key] = value;
        }

        public CollectionBotParameters(Dictionary<string, object>? otherParameters = null)
            => _otherParameters=otherParameters ?? [];
        public CollectionBotParameters(CollectionBotParameters? collectionBot)
            => _otherParameters=collectionBot?._otherParameters ?? [];

        public bool ContainsKey(string key) => _otherParameters.ContainsKey(key);
        public bool TryGetParameter<T>(string key, out T? value)
        {
            if (_otherParameters.TryGetValue(key, out object? valueO) && valueO is T valueR)
            {
                value = valueR;
                return true;
            }
            value = default;
            return false;
        }
    }
}
