public partial class Playlist
{
    public ItemDict Items { get; set; }

    public Playlist(Item[] items)
    {
        Items = new ItemDict();
        Populate(items);
    }

    private void Populate(Item[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            Items.Add(Items.Count, items[i]);

            if (items[i].Children != null)
                if (items[i].Children.Any())
                {
                    Populate(items[i].Children);
                }
        }
    }

    public class ItemDict : Dictionary<int, Item>
    {
        public Item this[string key]
        {
            get
            {
                foreach (var value in Values)
                {
                    if (value.Name == key)
                        return value;
                }

                throw new NullReferenceException(nameof(key));
            }
        }
    }
}
