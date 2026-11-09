if (args.Any(a => a is not ("--all-suites" or "--grid-laboratory")))
{
    Console.Error.WriteLine("R02: solo --all-suites y --grid-laboratory. Las suites geográficas antiguas están archivadas y NO se ejecutan.");
    return 2;
}
Console.WriteLine("R02 OFFICIAL: cuadrícula y laboratorio limpio. Motor, tests y baselines geográficos históricos NO EJECUTADOS; los fallos anteriores no se declaran resueltos.");
(string Name, Action Run)[] suites = [("Grid", GridLaboratoryTests.Grid), ("Laboratory", GridLaboratoryTests.Laboratory),
    ("TextEditing", GridLaboratoryTests.TextEditing), ("LegacyIsolation", GridLaboratoryTests.Isolation)];
int failures = 0;
foreach (var suite in suites)
{
    try { suite.Run(); Console.WriteLine("SUITE PASS: " + suite.Name); }
    catch (Exception error) { failures++; Console.Error.WriteLine($"SUITE FAIL: {suite.Name}: {error}"); }
}
return failures == 0 ? 0 : 1;
