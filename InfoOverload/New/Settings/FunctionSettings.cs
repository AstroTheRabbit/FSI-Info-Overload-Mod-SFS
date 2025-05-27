using System.Collections.Generic;

namespace InfoOverload.New.Settings
{
    public class FunctionSettings
    {
        public List<Setting> Settings { get; set; }
        public T GetValue<T>(string key)
        {
            foreach (Setting s in Settings)
            {
                if (s.Key == key) return (T) s.Value;
            }
            
            throw new KeyNotFoundException();
        }
    }
}