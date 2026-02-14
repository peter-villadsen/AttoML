# AttoML Examples

This directory contains example programs demonstrating AttoML's features.

## TextIO and HTTP Modules

These examples demonstrate the new I/O capabilities:

### `textio_demo.atto`
Comprehensive demonstration of the TextIO module:
- Console output with `TextIO.print`
- File writing with `openOut` and `output`
- File reading with `openIn` and `input`
- Appending with `openAppend`
- Standard streams (`stdOut`, `stdErr`)

**Run it:**
```bash
dotnet run --project src/AttoML.Interpreter examples/textio_demo.atto
```

### `http_demo.atto`
Comprehensive demonstration of the HTTP module:
- Simple GET requests
- POST with text body
- POST with JSON
- GET with custom headers

**Run it:**
```bash
dotnet run --project src/AttoML.Interpreter examples/http_demo.atto
```

### `api_to_file.atto`
Practical example combining both modules:
- Fetches data from a public API
- Saves the response to a file
- Reads it back and verifies
- Appends to a log file

**Run it:**
```bash
dotnet run --project src/AttoML.Interpreter examples/api_to_file.atto
```

## Output Files

These examples will create temporary files:
- `api_response.json` - JSON response from API
- `api_log.txt` - Log entries
- `demo_output.txt` - Test file output

These files are ignored by git (see `.gitignore`).

## More Examples

For more examples, see:
- **Documentation**: [docs/TextIO_and_HTTP.md](../docs/TextIO_and_HTTP.md)
- **Test Suite**: `tests/AttoML.Tests/` for comprehensive unit tests
