# wan24-Compression

This library exports a generic compression API, which allows to use any 
implemented compression algorithm to be applied using a simple interface:

```cs
// Compress memory
byte[] compressed = uncompressed.Compress();
uncompressed = compressed.Decompress();

// Compress a stream
uncompressedStream.Compress(compressedStream, new()
{
    LeaveOpen = true
});
uncompressedStream.SetLength(0);
compressedStream.Decompress(uncompressedStream);
```

## How to get it

This library is available as 
[NuGet package](https://www.nuget.org/packages/wan24-Compression/).
