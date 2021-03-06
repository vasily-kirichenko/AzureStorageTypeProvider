﻿module FSharp.Azure.StorageTypeProvider.``Blob Unit Tests``

open FSharp.Azure.StorageTypeProvider
open Microsoft.WindowsAzure.Storage.Blob
open Swensen.Unquote
open System.Linq
open Xunit

type Local = AzureTypeProvider<"DevStorageAccount", "">

let container = Local.Containers.``tp-test``

[<Fact>]
let ``Correctly identifies blob containers``() =
    // compiles!
    Local.Containers.``tp-test``

[<Fact>]
let ``Correctly identifies blobs in a container``() =
    // compiles!
    [ container .``file1.txt``
      container .``file2.txt``
      container .``file3.txt`` ]

[<Fact>]
let ``Correctly identifies blobs in a subfolder``() =
    // compiles!
    container .``folder/``.``childFile.txt``

[<Fact>]
let ``Correctly gets size of a blob``() =
    container .``sample.txt``.Size =? 52L

[<Fact>]
let ``Reads a text file as text``() =
    let text = container .``sample.txt``.Read()
    text =? "the quick brown fox jumped over the lazy dog\nbananas"

[<Fact>]
let ``Streams a text file line-by-line``() =
    let textStream = container .``sample.txt``.OpenStreamAsText()
    let text = seq { while not textStream.EndOfStream do
                        yield textStream.ReadLine() }
               |> Seq.toArray
    text.[0] =? "the quick brown fox jumped over the lazy dog"
    text.[1] =? "bananas"
    text.Length =? 2

[<Fact>]
let ``Opens a file with xml extension as an XML document``() =
    let document = container.``data.xml``.ReadAsXDocument()
    let value = document.Elements().First()
                        .Elements().First()
                        .Value
    value =? "thing"

[<Fact>]
let ``Cloud Blob Client relates to the same data as the type provider``() =
    (Local.Containers.CloudBlobClient.ListContainers()
     |> Seq.map(fun c -> c.Name)
     |> Set.ofSeq
     |> Set.contains "tp-test") =? true

[<Fact>]
let ``Cloud Blob Container relates to the same data as the type provider``() =
    let client = container.AsCloudBlobContainer()
    let blobs = client.ListBlobs() |> Seq.choose(function | :? CloudBlockBlob as b -> Some b | _ -> None) |> Seq.map(fun c -> c.Name) |> Seq.toList
    blobs =? [ "data.xml"; "file1.txt"; "file2.txt"; "file3.txt"; "sample.txt" ]

[<Fact>]
let ``Cloud Block Blob relates to the same data as the type provider``() =
    let blob = container.``data.xml``.AsCloudBlockBlob()
    blob.Name =? "data.xml"