# wan24-Compression

This library exports a generic compression API, which allows to use any 
implemented compression algorithm to be applied using a simple interface.

The goals of this library are:

- Make a choice being a less torture
- Make a complex thing as easy as possible

Implementing (new) compression algorithms into (existing) code can be 
challenging. `wan24-Compression` tries to make it as easy as possible, while 
the API is still complex due to the huge number of options it offers.

## How to get it

This library is available as 
[NuGet package](https://www.nuget.org/packages/wan24-Compression/).

## Usage

In case you don't use the `wan24-Core` bootstrapper logic, you need to 
initialize the library first, before you can use it:

```cs
wan24.Compression.Bootstrapper.Boot();
```

This will initialize the `wan24-Compression` library.

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

## Decompression length limit

To avoid compressed bombs, which are tiny, but decompress to huge amounts of 
data, which may DoS your app, you should set a maximum for the uncompressed 
data size, if possible:

```cs
CompressionOptions options = new CompressionOptions()
    .WithMaxUncompressedDataLength(12345);// Limit to 12345 byte
```

This limits the decompressed data length effective, even if the uncompressed 
length is unknown.

**NOTE**: This limit won't affect compression and is being applied only for 
decompression! Anyway, it's a security task to set a decompression length 
limit.

## JSON configuration

You could implement a JSON configuration file using the `AppConfig` logic from 
`wan24-Core`, and the `CompressionAppConfig`. There it's possible to define 
disabled algorithms, which makes it possible to react to an unwanted algorithm 
very fast, at any time and without having to update your app, for example. If 
you use an `AppConfig`, it could look like this:

```cs
public class YourAppConfig : AppConfig
{
    public YourAppConfig() : base() { }

    [AppConfig(AfterBootstrap = true)]
    public CompressionAppConfig? Compression { get; set; }
}

await AppConfig.LoadAsync<YourAppConfig>();
```

In the `config.json` in your app root folder:

```json
{
    "Compression":{
        ...
    }
}
```

Anyway, you could also place and load a `CompressionAppConfig` in any 
configuration which supports using that custom type.
