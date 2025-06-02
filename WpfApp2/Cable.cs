namespace WpfApp2
{
    public class Cable
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public double OD { get; set; }
        public bool IsTriplex { get; set; }
        public double GroundOD { get; set; }

        public Cable(string id, string name, double od, bool isTriplex, double groundOD)
        {
            ID = id;
            Name = name;
            OD = od;
            IsTriplex = isTriplex;
            GroundOD = groundOD;
        }
    }
}
