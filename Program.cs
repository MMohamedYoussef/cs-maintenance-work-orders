using System.Globalization;
using Microsoft.VisualBasic.FileIO;

namespace WorkOrders;

public static class Program
{
    private sealed record Order(string Name, string Asset, int Severity, int Hours, string Skill, int Priority);
    private sealed class Technician(string name, string skill, int capacity)
    {
        public string Name { get; } = name;
        public string Skill { get; } = skill;
        public int Remaining { get; set; } = capacity;
    }

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length != 4) throw new ArgumentException("usage: WorkOrders ASSETS.csv ORDERS.csv TECHNICIANS.csv NEW_OUTPUT.csv");
            var assets = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var row in ReadCsv(args[0], "asset,criticality"))
            {
                var name = Id(row[0], "asset");
                if (!assets.TryAdd(name, Positive(row[1], "criticality", 5))) throw new InvalidDataException($"duplicate asset: {name}");
            }
            if (assets.Count == 0) throw new InvalidDataException("assets are empty");
            var orders = new List<Order>(); var orderIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in ReadCsv(args[1], "order,asset,severity,hours,skill"))
            {
                var name = Id(row[0], "order"); var asset = Id(row[1], "asset");
                if (!orderIds.Add(name)) throw new InvalidDataException($"duplicate order: {name}");
                if (!assets.TryGetValue(asset, out var criticality)) throw new InvalidDataException($"unknown asset: {asset}");
                var severity = Positive(row[2], "severity", 5);
                var hours = Positive(row[3], "hours", 1_000_000);
                var skill = Id(row[4], "skill");
                orders.Add(new Order(name, asset, severity, hours, skill, criticality * severity));
            }
            if (orders.Count == 0) throw new InvalidDataException("orders are empty");
            var techs = new List<Technician>(); var techIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in ReadCsv(args[2], "technician,skill,capacity_hours"))
            {
                var name = Id(row[0], "technician");
                if (!techIds.Add(name)) throw new InvalidDataException($"duplicate technician: {name}");
                techs.Add(new Technician(name, Id(row[1], "skill"), Nonnegative(row[2], "capacity hours", 1_000_000)));
            }
            if (techs.Count == 0) throw new InvalidDataException("technicians are empty");
            if (File.Exists(args[3])) throw new IOException($"output already exists: {args[3]}");
            using var writer = new StreamWriter(new FileStream(args[3], FileMode.CreateNew, FileAccess.Write));
            writer.NewLine = "\n";
            writer.WriteLine("order,priority,technician,status");
            foreach (var order in orders.OrderByDescending(o => o.Priority).ThenBy(o => o.Name, StringComparer.Ordinal))
            {
                var tech = techs.Where(t => t.Skill == order.Skill && t.Remaining >= order.Hours).OrderBy(t => t.Name, StringComparer.Ordinal).FirstOrDefault();
                if (tech is null) writer.WriteLine($"{order.Name},{order.Priority},,unassigned");
                else { tech.Remaining -= order.Hours; writer.WriteLine($"{order.Name},{order.Priority},{tech.Name},assigned"); }
            }
            return 0;
        }
        catch (Exception e) when (e is ArgumentException or InvalidDataException or IOException or UnauthorizedAccessException)
        { Console.Error.WriteLine($"WorkOrders: {e.Message}"); return 2; }
    }
    private static string Id(string value, string name)
    {
        if (value.Length == 0 || value.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_')) throw new InvalidDataException($"invalid {name}: {value}");
        return value;
    }
    private static int Positive(string value, string name, int max)
    {
        var number = Nonnegative(value, name, max);
        if (number == 0) throw new InvalidDataException($"{name} must be positive: {value}");
        return number;
    }
    private static int Nonnegative(string value, string name, int max)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number < 0 || number > max)
            throw new InvalidDataException($"invalid {name} (0..{max}): {value}");
        return number;
    }
    private static List<string[]> ReadCsv(string path, string header)
    {
        using var parser = new TextFieldParser(path); parser.SetDelimiters(","); parser.HasFieldsEnclosedInQuotes = true;
        var rows = new List<string[]>();
        try
        {
            var actual = parser.ReadFields();
            if (actual is null || string.Join(',', actual) != header) throw new InvalidDataException($"expected CSV header: {header}");
            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields();
                if (fields is null || fields.Length != actual.Length) throw new InvalidDataException($"wrong column count near line {parser.LineNumber}");
                rows.Add(fields);
            }
        }
        catch (MalformedLineException e) { throw new InvalidDataException($"malformed CSV near line {e.LineNumber}", e); }
        return rows;
    }
}
