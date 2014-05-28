namespace Regard.Query.Flat
{
    /// <summary>
    /// Granular actions that can be applied to an event being processed
    /// </summary>
    public interface IPipelineAction
    {
        /// <summary>
        /// Drop the event (don't process any more)
        /// </summary>
        void Drop();

        /// <summary>
        /// Place the event in a named bucket
        /// </summary>
        void Bucket(string name);

        /// <summary>
        /// Adds one to the value of a field in the current bucket
        /// </summary>
        /// <param name="fieldName">The field name within the bucket that </param>
        void Count(string fieldName);

        /// <summary>
        /// Adds one to the value of a field if a particular key has not been seen before within the current bucket
        /// </summary>
        /// <param name="fieldName">The field to add to</param>
        /// <param name="key">The key that should not have been seen before</param>
        void CountUnique(string fieldName, string key);

        /// <summary>
        /// Adds a value to a field in a bucket
        /// </summary>
        /// <param name="fieldName">The name of the field to add to</param>
        /// <param name="value">The value to add to the field</param>
        void Sum(string fieldName, double value);
    }
}
