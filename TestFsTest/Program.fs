open System
open System.Net.Http
open System.Threading.Tasks
open System.Threading
open FSharp.Core
open System
open System.IO
open System.Linq
open FSharp.Control
open System.Security.Cryptography
open System.Text
open FSharp.NativeInterop
open System.Buffers
open System.Net.Sockets
open System.Security.Authentication
open System.Net.Security
open System.Net
open System.Net.Http.Headers

open System.Text.RegularExpressions

[<EntryPoint>]
let main argv =
    
    TaskScheduler.UnobservedTaskException.Add(fun evArgs -> Console.WriteLine(evArgs))



    Task.FromException(new ArgumentException())|> ignore


    GC.Collect();
    GC.Collect();
    GC.Collect();
    GC.Collect();


    while true do
        ()
    Console.ReadLine()|>ignore
    0