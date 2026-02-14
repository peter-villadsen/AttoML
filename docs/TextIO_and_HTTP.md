# TextIO and HTTP Modules

AttoML now supports file I/O and HTTP operations through two new builtin modules: `TextIO` and `Http`.

## TextIO Module

The TextIO module provides Standard ML-style file and console I/O operations.

### Functions

#### Console I/O

- **`TextIO.print : string -> unit`**
  - Prints a string to stdout and flushes the output
  - Example: `TextIO.print "Hello, World!\n"`

#### Standard Streams

- **`TextIO.stdIn : instream`** - Standard input stream
- **`TextIO.stdOut : outstream`** - Standard output stream
- **`TextIO.stdErr : outstream`** - Standard error stream

#### File Operations

- **`TextIO.openIn : string -> instream`**
  - Opens a file for reading
  - Raises `Io` exception if file cannot be opened
  - Example: `let inStream = TextIO.openIn "data.txt" in ...`

- **`TextIO.openOut : string -> outstream`**
  - Opens/creates a file for writing (truncates existing file)
  - Raises `Io` exception if file cannot be opened
  - Example: `let outStream = TextIO.openOut "output.txt" in ...`

- **`TextIO.openAppend : string -> outstream`**
  - Opens a file for appending (preserves existing content)
  - Raises `Io` exception if file cannot be opened
  - Example: `let appendStream = TextIO.openAppend "log.txt" in ...`

- **`TextIO.closeIn : instream -> unit`**
  - Closes an input stream
  - Example: `TextIO.closeIn inStream`

- **`TextIO.closeOut : outstream -> unit`**
  - Closes an output stream
  - Example: `TextIO.closeOut outStream`

#### Reading

- **`TextIO.input : instream -> string`**
  - Reads the entire remaining content of a stream
  - Example:
    ```ocaml
    let inStream = TextIO.openIn "file.txt" in
    let content = TextIO.input inStream in
    let _ = TextIO.closeIn inStream in
    content
    ```

- **`TextIO.inputLine : instream -> string option`**
  - Reads a single line from the stream
  - Returns `Some line` if a line was read, `None` at EOF
  - Lines include the newline character
  - **Pattern matching works perfectly!**
  - Example:
    ```ocaml
    let inStream = TextIO.openIn "file.txt" in
    let line = TextIO.inputLine inStream in
    let text = match line with
      Some l -> l
    | None -> "EOF\n"
    end in
    let _ = TextIO.closeIn inStream in
    text
    ```

#### Writing

- **`TextIO.output : outstream -> string -> unit`**
  - Writes a string to an output stream
  - Example: `TextIO.output outStream "Hello\n"`

- **`TextIO.flushOut : outstream -> unit`**
  - Flushes the output buffer
  - Example: `TextIO.flushOut TextIO.stdOut`

### Complete Example

```ocaml
(* Write to a file *)
let _ = TextIO.print "Writing to file...\n" in
let outStream = TextIO.openOut "output.txt" in
let _ = TextIO.output outStream "Line 1\n" in
let _ = TextIO.output outStream "Line 2\n" in
let _ = TextIO.closeOut outStream in

(* Read from a file *)
let _ = TextIO.print "Reading from file...\n" in
let inStream = TextIO.openIn "output.txt" in
let content = TextIO.input inStream in
let _ = TextIO.closeIn inStream in
let _ = TextIO.print "File contents:\n" in
let _ = TextIO.print content in

(* Append to a file *)
let appendStream = TextIO.openAppend "output.txt" in
let _ = TextIO.output appendStream "Line 3 (appended)\n" in
TextIO.closeOut appendStream
```

## HTTP Module

The HTTP module provides basic HTTP client functionality for making web requests.

### Functions

- **`Http.get : string -> string`**
  - Makes an HTTP GET request
  - Returns the response body as a string
  - Raises `Http` exception on failure
  - Example:
    ```ocaml
    let response = Http.get "https://api.example.com/data" in
    TextIO.print response
    ```

- **`Http.post : string -> string -> string`**
  - Makes an HTTP POST request with a text body
  - First argument: URL
  - Second argument: request body
  - Returns the response body as a string
  - Content-Type is set to "text/plain"
  - Raises `Http` exception on failure
  - Example:
    ```ocaml
    let response = Http.post "https://api.example.com/submit" "data to send" in
    TextIO.print response
    ```

- **`Http.postJson : string -> string -> string`**
  - Makes an HTTP POST request with JSON data
  - First argument: URL
  - Second argument: JSON string
  - Returns the response body as a string
  - Content-Type is set to "application/json"
  - Raises `Http` exception on failure
  - Example:
    ```ocaml
    let jsonData = "{\"key\": \"value\", \"number\": 42}" in
    let response = Http.postJson "https://api.example.com/json" jsonData in
    TextIO.print response
    ```

- **`Http.getWithHeaders : string -> (string * string) list -> string`**
  - Makes an HTTP GET request with custom headers
  - First argument: URL
  - Second argument: list of header name/value tuples
  - Returns the response body as a string
  - Raises `Http` exception on failure
  - Example:
    ```ocaml
    let headers = [
      ("User-Agent", "AttoML/1.0"),
      ("Authorization", "Bearer token123"),
      ("X-Custom-Header", "value")
    ] in
    let response = Http.getWithHeaders "https://api.example.com/data" headers in
    TextIO.print response
    ```

### Complete Example

```ocaml
(* GET request *)
let _ = TextIO.print "Making GET request...\n" in
let response = Http.get "https://httpbin.org/get" in
let _ = TextIO.print "Response:\n" in
let _ = TextIO.print response in
let _ = TextIO.print "\n\n" in

(* POST JSON *)
let _ = TextIO.print "Making POST request with JSON...\n" in
let jsonData = "{\"message\": \"Hello from AttoML\"}" in
let postResponse = Http.postJson "https://httpbin.org/post" jsonData in
let _ = TextIO.print "Response:\n" in
let _ = TextIO.print postResponse in
let _ = TextIO.print "\n\n" in

(* GET with headers *)
let _ = TextIO.print "Making GET request with custom headers...\n" in
let headers = [("User-Agent", "AttoML/1.0")] in
let headerResponse = Http.getWithHeaders "https://httpbin.org/get" headers in
let _ = TextIO.print "Response:\n" in
TextIO.print headerResponse
```

## Error Handling

Both modules raise exceptions on errors:

- **TextIO**: Raises `Io` exception with an error message
  ```ocaml
  handle
    TextIO.openIn "nonexistent.txt"
  with
    Io msg -> TextIO.print ("Error: " ^ msg)
  ```

- **HTTP**: Raises `Http` exception with an error message
  ```ocaml
  handle
    Http.get "https://invalid-url-that-does-not-exist.com"
  with
    Http msg -> TextIO.print ("HTTP Error: " ^ msg)
  ```

## Implementation Details

### Stream Types

- `instream` and `outstream` are opaque types
- Streams are managed internally with unique IDs
- Standard streams (stdIn, stdOut, stdErr) use special negative IDs
- File streams are automatically tracked and can be closed

### HTTP Client

- Uses .NET's `HttpClient` for all requests
- Operations are synchronous (block until complete)
- Responses are returned as strings
- Only response bodies are returned (status codes and headers are not accessible)

## Known Limitations

1. **Binary I/O**: Only text mode is supported. Binary file operations are not available.

2. **Async Operations**: All operations are synchronous and will block.

3. **HTTP Response Details**: Only response bodies are returned. Status codes, headers, and other metadata are not accessible.

4. **Stream Safety**: Streams must be manually closed. There is no automatic cleanup or finalizers.

## Future Enhancements

Potential future improvements:

- Binary I/O (BinIO module)
- Async HTTP operations
- HTTP response status codes and headers
- WebSocket support
- File system operations (directory listing, delete, rename, etc.)
- Automatic stream cleanup with finalizers
- Streaming I/O (reading/writing in chunks)
