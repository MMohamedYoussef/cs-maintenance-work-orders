var failed = 0;
void Test(string name, Action check) { try { check(); Console.WriteLine($"PASS {name}"); } catch (Exception e) { failed++; Console.WriteLine($"FAIL {name}: {e.Message}"); } }
string Temp() { var p = Path.Combine(Path.GetTempPath(), "work-orders-test-" + Guid.NewGuid()); Directory.CreateDirectory(p); return p; }
void Equal(string expected, string actual) { if (expected != actual) throw new Exception($"expected [{expected}], got [{actual}]"); }
Test("prioritizes risk and respects skill and capacity", () => {
    var d = Temp();
    File.WriteAllText(Path.Combine(d,"assets.csv"), "asset,criticality\nPump,5\nFan,2\n");
    File.WriteAllText(Path.Combine(d,"orders.csv"), "order,asset,severity,hours,skill\nLow,Fan,2,2,electrical\nHigh,Pump,5,3,mechanical\nNext,Pump,4,2,mechanical\n");
    File.WriteAllText(Path.Combine(d,"techs.csv"), "technician,skill,capacity_hours\nAmy,mechanical,3\nBob,mechanical,2\nCal,electrical,1\n");
    var output = Path.Combine(d,"out.csv");
    Equal("0", WorkOrders.Program.Main([Path.Combine(d,"assets.csv"),Path.Combine(d,"orders.csv"),Path.Combine(d,"techs.csv"),output]).ToString());
    Equal("order,priority,technician,status\nHigh,25,Amy,assigned\nNext,20,Bob,assigned\nLow,4,,unassigned\n",File.ReadAllText(output).Replace("\r\n","\n"));
});
Test("rejects unknown asset without output", () => {
    var d = Temp(); File.WriteAllText(Path.Combine(d,"assets.csv"),"asset,criticality\nPump,5\n");
    File.WriteAllText(Path.Combine(d,"orders.csv"),"order,asset,severity,hours,skill\nBad,Other,2,1,mechanical\n");
    File.WriteAllText(Path.Combine(d,"techs.csv"),"technician,skill,capacity_hours\nAmy,mechanical,8\n");
    var output = Path.Combine(d,"out.csv");
    if (WorkOrders.Program.Main([Path.Combine(d,"assets.csv"),Path.Combine(d,"orders.csv"),Path.Combine(d,"techs.csv"),output]) == 0 || File.Exists(output)) throw new Exception("unknown asset accepted");
});
Test("rejects negative or zero work hours", () => {
    var d = Temp(); File.WriteAllText(Path.Combine(d,"assets.csv"),"asset,criticality\nPump,5\n");
    File.WriteAllText(Path.Combine(d,"orders.csv"),"order,asset,severity,hours,skill\nBad,Pump,2,0,mechanical\n");
    File.WriteAllText(Path.Combine(d,"techs.csv"),"technician,skill,capacity_hours\nAmy,mechanical,8\n");
    if (WorkOrders.Program.Main([Path.Combine(d,"assets.csv"),Path.Combine(d,"orders.csv"),Path.Combine(d,"techs.csv"),Path.Combine(d,"out.csv")]) == 0) throw new Exception("zero hours accepted");
});
Console.WriteLine($"{3 - failed}/3 passed"); if (failed > 0) Environment.Exit(1);
