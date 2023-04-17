# wan24-Compression

This library exports a generic compression API, which allows to use any 
implemented compression algorithm to be applied using a simple interface:

```cs
byte[] compressed = uncompressed.Compress();
uncompressed = compressed.Decompress();
```

There are extension methods for memory and streams.

## How to get it

With the first release, this library will be available as NuGet package, too. 
Unless published, you can compile the DLL using the source project.
