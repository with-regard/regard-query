namespace Regard.Query.Flat
{
    /// <summary>
    /// Class representing an Azure table entity key
    /// </summary>
    class EntityKey
    {
        public EntityKey(string partitionKey, string rowKey)
        {
            PartitionKey    = partitionKey;
            RowKey          = rowKey;
        }

        public string PartitionKey { get; private set; }
        public string RowKey { get; private set; }

        protected bool Equals(EntityKey other)
        {
            return string.Equals(PartitionKey, other.PartitionKey) && string.Equals(RowKey, other.RowKey);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EntityKey) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((PartitionKey != null ? PartitionKey.GetHashCode() : 0)*397) ^ (RowKey != null ? RowKey.GetHashCode() : 0);
            }
        }
    }
}
