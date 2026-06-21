#pragma warning disable
namespace DataEditorX
{
    class MyComparer<K> : IComparer<K>
    {
        public int Compare(K x, K y)
        {
            return 1;   // Never equal, allowing duplicate keys.
        }
    }

    public class MySortList<K, V> : SortedList<K, V>
    {

        public MySortList() : base(new MyComparer<K>())
        {
        }

        public new void Add(K key, V value)
        {
            // Used to skip duplicate key/value pairs.
            int flag = 0;
            // Check whether the same key/value pair already exists.
            foreach (KeyValuePair<K, V> item in this)
            {
                if (item.Key.ToString() == key.ToString() && item.Value.ToString() == value.ToString())
                {
                    flag = 1;
                }
            }
            if (flag == 1)
            {
                return;  // Skip duplicate pair.
            }
            // Otherwise add the new pair.
            base.Add(key, value);
        }
    }
}
