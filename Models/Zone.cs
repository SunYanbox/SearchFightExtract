using System.Collections.Generic;

namespace SearchFightExtract
{
    class Zone
    {
        public string Name { get; set; }
        public List<int> Neighbors { get; set; } = new List<int>();
        public bool Explored { get; set; }
        public bool Searched { get; set; }
        public bool IsExtract { get; set; }
        public Enemy Guard { get; set; }
        public bool HasEvent { get; set; }
        public string EventDesc { get; set; }
    }
}
