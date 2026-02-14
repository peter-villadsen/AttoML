using System;
using System.Collections.Generic;
using System.IO;
using AttoML.Interpreter.Runtime;

namespace AttoML.Interpreter.Builtins
{
    /// <summary>
    /// Low-level TextIO implementation module.
    /// Returns simple types (lists) instead of ADTs to avoid type system conflicts.
    /// The high-level TextIO wrapper (in Prelude) provides the idiomatic ML API.
    /// </summary>
    public static class TextIOImplementationModule
    {
        private static int _nextStreamId = 1;
        private static readonly Dictionary<int, StreamReader> _inputStreams = new();
        private static readonly Dictionary<int, StreamWriter> _outputStreams = new();

        // Special stream IDs
        private const int STDIN_ID = -1;
        private const int STDOUT_ID = -2;
        private const int STDERR_ID = -3;

        public static ModuleVal Build()
        {
            var m = new Dictionary<string, Value>();

            // Standard streams
            m["stdIn"] = new StreamVal(STDIN_ID, "in");
            m["stdOut"] = new StreamVal(STDOUT_ID, "out");
            m["stdErr"] = new StreamVal(STDERR_ID, "out");

            // Print function
            m["print"] = new ClosureVal(s =>
            {
                Console.Out.Write(((StringVal)s).Value);
                Console.Out.Flush();
                return UnitVal.Instance;
            });

            // File opening functions
            m["openIn"] = new ClosureVal(filename =>
            {
                try
                {
                    var path = ((StringVal)filename).Value;
                    var reader = new StreamReader(path);
                    var id = _nextStreamId++;
                    _inputStreams[id] = reader;
                    return new StreamVal(id, "in");
                }
                catch (Exception ex)
                {
                    throw new AttoException(new AdtVal("Io",
                        new StringVal($"Cannot open {((StringVal)filename).Value}: {ex.Message}")));
                }
            });

            m["openOut"] = new ClosureVal(filename =>
            {
                try
                {
                    var path = ((StringVal)filename).Value;
                    var writer = new StreamWriter(path, false); // truncate
                    var id = _nextStreamId++;
                    _outputStreams[id] = writer;
                    return new StreamVal(id, "out");
                }
                catch (Exception ex)
                {
                    throw new AttoException(new AdtVal("Io",
                        new StringVal($"Cannot open {((StringVal)filename).Value}: {ex.Message}")));
                }
            });

            m["openAppend"] = new ClosureVal(filename =>
            {
                try
                {
                    var path = ((StringVal)filename).Value;
                    var writer = new StreamWriter(path, true); // append
                    var id = _nextStreamId++;
                    _outputStreams[id] = writer;
                    return new StreamVal(id, "out");
                }
                catch (Exception ex)
                {
                    throw new AttoException(new AdtVal("Io",
                        new StringVal($"Cannot open {((StringVal)filename).Value}: {ex.Message}")));
                }
            });

            // Close functions
            m["closeIn"] = new ClosureVal(stream =>
            {
                var sv = (StreamVal)stream;
                if (sv.Id >= 0 && _inputStreams.TryGetValue(sv.Id, out var reader))
                {
                    reader.Close();
                    _inputStreams.Remove(sv.Id);
                }
                return UnitVal.Instance;
            });

            m["closeOut"] = new ClosureVal(stream =>
            {
                var sv = (StreamVal)stream;
                if (sv.Id >= 0 && _outputStreams.TryGetValue(sv.Id, out var writer))
                {
                    writer.Close();
                    _outputStreams.Remove(sv.Id);
                }
                return UnitVal.Instance;
            });

            // Input functions
            // inputLine returns string list: [] = EOF, [line] = data
            m["inputLine"] = new ClosureVal(stream =>
            {
                var sv = (StreamVal)stream;
                StreamReader? reader = null;

                if (sv.Id == STDIN_ID)
                {
                    var line = Console.ReadLine();
                    if (line == null)
                        return new ListVal(Array.Empty<Value>()); // EOF = []
                    else
                        return new ListVal(new[] { new StringVal(line + "\n") }); // Data = [line]
                }
                else if (!_inputStreams.TryGetValue(sv.Id, out reader))
                {
                    throw new AttoException(new AdtVal("Io",
                        new StringVal("Invalid input stream")));
                }

                var lineFromFile = reader.ReadLine();
                if (lineFromFile == null)
                    return new ListVal(Array.Empty<Value>()); // EOF = []
                else
                    return new ListVal(new[] { new StringVal(lineFromFile + "\n") }); // Data = [line]
            });

            m["input"] = new ClosureVal(stream =>
            {
                var sv = (StreamVal)stream;
                StreamReader? reader = null;

                if (sv.Id == STDIN_ID)
                {
                    // Read all from stdin until EOF
                    var text = Console.In.ReadToEnd();
                    return new StringVal(text);
                }
                else if (!_inputStreams.TryGetValue(sv.Id, out reader))
                {
                    throw new AttoException(new AdtVal("Io",
                        new StringVal("Invalid input stream")));
                }

                return new StringVal(reader.ReadToEnd());
            });

            // Output functions
            m["output"] = Curry2((stream, str) =>
            {
                var sv = (StreamVal)stream;
                var text = ((StringVal)str).Value;

                if (sv.Id == STDOUT_ID)
                {
                    Console.Out.Write(text);
                }
                else if (sv.Id == STDERR_ID)
                {
                    Console.Error.Write(text);
                }
                else if (!_outputStreams.TryGetValue(sv.Id, out var writer))
                {
                    throw new AttoException(new AdtVal("Io",
                        new StringVal("Invalid output stream")));
                }
                else
                {
                    writer.Write(text);
                }

                return UnitVal.Instance;
            });

            m["flushOut"] = new ClosureVal(stream =>
            {
                var sv = (StreamVal)stream;

                if (sv.Id == STDOUT_ID)
                    Console.Out.Flush();
                else if (sv.Id == STDERR_ID)
                    Console.Error.Flush();
                else if (_outputStreams.TryGetValue(sv.Id, out var writer))
                    writer.Flush();

                return UnitVal.Instance;
            });

            return new ModuleVal(m);
        }

        private static Value Curry2(Func<Value, Value, Value> f)
        {
            return new ClosureVal(a => new ClosureVal(b => f(a, b)));
        }
    }
}
