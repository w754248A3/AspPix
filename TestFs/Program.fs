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
open LinqToDB;
open LinqToDB.Mapping;
open LinqToDB.Data
open System.Text.RegularExpressions
open System.Threading.Channels
open System.Collections
open LinqToDB.Common

module PixSql =

    type Pixiv2 = {
        
        [<PrimaryKey>]
        Id:int
        Mark:int
        Date:DateTime
        ImgEN:byte
    }
    
    type PixivTag = {
        [<PrimaryKey>]
        Id:int
        Tag:string
    }

    type PixivTagHas = {
        
        [<PrimaryKey>]
        ItemId:int

        [<PrimaryKey>]
        TagId:int
    }

[<EntryPoint>]
let main argv =
    
    Configuration.Linq.GuardGrouping <- false;
    let db = new DataConnection(LinqToDB.ProviderName.MySql, "Host=192.168.0.101;Port=3306;User=myuser;Password=mypass;Database=mysql;SslMode=none")

    let a = query{
            
            for item in db.GetTable<PixSql.Pixiv2>() do
            groupBy item.Mark into g
            select {|Mark = g.Key; Count = g.Count()|}
            take 100
    }

    let vs = a.ToArray();


    
    Array.ForEach(vs, fun e-> Console.WriteLine(e.Count));



    0