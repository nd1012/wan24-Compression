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

The goals of this library are:

- Make a choice being a less torture
- Make a complex thing as easy as possible

Implementing (new) compression algorithms into (existing) code can be 
challenging. `wan24-Compression` tries to make it as easy as possible, while 
the API is still complex due to the huge number of options it offers.

**NOTE**: The compressed output of this library may include a header, which 
can't (yet) be interpreted by any third party vendor code. That means, a 
compressed output of this library can't be decompressed with a third party 
compression library, even this library implements standard compression 
algorithms.

Using this library for a compressed sequence which has to be exchanged with a 
third party application, which relies on working with standard compression 
algorithm output, is not recommended - it may not work!

Anyway, this library should be a good choice for isolated use within your 
application(s), if want to avoid a hussle with implementing newer compression 
algorithms.

## How to get it

This library is available as 
[NuGet package](https://www.nuget.org/packages/wan24-Compression/).
