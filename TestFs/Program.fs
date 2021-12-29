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

    
    type PixivData = {
        
        [<PrimaryKey>]
        Id:int
        Mark:int
        Date:DateTime
        Flags:int
    }
    
    type PixivTag = {
        [<PrimaryKey>]
        Id:int
        Tag:string
    }

    type PixivTagMap = {
        
        [<PrimaryKey>]
        ItemId:int

        [<PrimaryKey>]
        TagId:int
    }


let test() =
    Configuration.Linq.GuardGrouping <- false;
    let db = new DataConnection(LinqToDB.ProviderName.MySql, "Host=192.168.0.101;Port=3306;User=myuser;Password=mypass;Database=mysql;SslMode=none")

    let a = query{
            
            for item in db.GetTable<PixSql.PixivData>() do
            groupBy item.Mark into g
            select {|Mark = g.Key; Count = g.Count()|}
            take 100
    }

    let date = DateTime.Now.Add(TimeSpan(-7,0,0,0))
    let pix = query{
        for pixiv2 in db.GetTable<PixSql.PixivData>() do
        where (pixiv2.Date > date)
        sortByDescending pixiv2.Mark
        take 1000
        join hasTag in db.GetTable<PixSql.PixivTagMap>()
            on (pixiv2.Id = hasTag.ItemId)
        join tagid in db.GetTable<PixSql.PixivTag>()
            on (hasTag.TagId = tagid.Id)
        groupBy tagid.Id into g
        select {|Id = g.Key; Count = g.Count() |} into v
        join tag in db.GetTable<PixSql.PixivTag>()
            on (v.Id = tag.Id)
        sortByDescending v.Count;
        select {|Id = v.Id; Tag = tag.Tag; Count = v.Count|}
        take 150
    }






    let vs = a.ToArray();


    
    Array.ForEach(vs, fun e-> Console.WriteLine(e.Count));



[<EntryPoint>]
let main argv =
    let db = new DataConnection(LinqToDB.ProviderName.MySql, "Host=192.168.0.101;Port=3306;User=myuser;Password=mypass;Database=mysql;SslMode=none")
    
    
    let a = db.CreateTable<PixSql.PixivData>();
    let b = db.CreateTable<PixSql.PixivTagMap>();
    let c = db.CreateTable<PixSql.PixivTag>();



    0