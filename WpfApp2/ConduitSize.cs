namespace WpfApp2
{
    public class ConduitSize
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public double AreaIN { get; set; }

        public ConduitSize(string name, string type, double areaIN)
        {
            Name = name;
            Type = type;
            AreaIN = areaIN;
        }
    }
}
