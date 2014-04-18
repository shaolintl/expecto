﻿namespace Fuchu

open System
open global.PerfUtil

[<AutoOpen; CompilationRepresentationAttribute(CompilationRepresentationFlags.ModuleSuffix)>]
module FuchuPerfUtil =
    open Fuchu
    open Fuchu.Helpers
    open Fuchu.Impl

    type PerfConfig =
        { a : string }

    type CompareConfig =
        { throwOnError : bool
          comparer     : IPerformanceComparer
          verbose      : bool }
        static member Defaults =
            { throwOnError = true
              comparer     = MeanComparer()
              verbose      = true }

    let perfTest name (testImpl : 'a -> unit when 'a :> ITestable) =
        { PerfTest.Id = name
          Test        = testImpl }

    let testPerfCompareWithConfig (conf : CompareConfig) name (subject : 'a) (alternatives : 'a list) (tests : PerfTest<'a> list) =
        let tester () =
            new ImplemantationComparer<_>(subject, alternatives, conf.comparer, conf.verbose, conf.throwOnError)
                :> PerformanceTester<'a>

        testCase name <| fun _ ->
            let results = PerfTest.run tester tests
            () // TODO: handle saving of results

    let testPerfCompare<'a when 'a :> ITestable> name (subj : 'a) (alts : 'a list) (tests : 'a PerfTest list) =
        testPerfCompareWithConfig CompareConfig.Defaults name subj alts tests

    let testPerfHistoryWithConfig config name =
        ()

    let testPerfHistory name =
        ()

module Usage =
    open global.PerfUtil

    module Types =
        type Y = { a : string; b : int }
        type X = { c : Nullable<int>; d : uint64 }

    type Serialiser =
        inherit ITestable
        abstract member Serialise<'a> : 'a -> unit

    type SlowSerialiser() =
        interface ITestable with
            member x.Name = "Slow Serialiser"
        interface Serialiser with
            member x.Serialise _ =
                System.Threading.Thread.Sleep(30)

    type FastSerialiser() =
        interface ITestable with
            member x.Name = "Fast Serialiser"
        interface Serialiser with
            member x.Serialise _ =
                System.Threading.Thread.Sleep(10)

    type FastSerialiserAlt() =
        interface ITestable with
            member x.Name = "Fast Serialiser Alt"
        interface Serialiser with
            member x.Serialise _ =
                System.Threading.Thread.Sleep(20)

    let alts : Serialiser list = [ FastSerialiser(); FastSerialiserAlt() ]
    let subj = SlowSerialiser() :> Serialiser

    open Types

    let tests =
        testList "performance tests" [
            testPerfCompare "implementations of Serialiser" subj alts [
                perfTest "serialising string" <| fun s ->
                    s.Serialise("wowowow")
                perfTest "serialising record" <| fun s ->
                    s.Serialise { a = "hello world"; b = 42 }
                ]
            ]

(* EXAMPLES:


    type ``Serializer Comparison`` () =
        inherit NUnitPerf<ISerializer>()

        let fsp = testSerializer :> ISerializer
        let bfs = new BinaryFormatterSerializer() :> ISerializer
        let ndc = new NetDataContractSerializer() :> ISerializer
        let jdn = new JsonDotNetSerializer() :> ISerializer
        let pbn = new ProtoBufSerializer() :> ISerializer
        let ssj = new ServiceStackJsonSerializer() :> ISerializer
        let sst = new ServiceStackTypeSerializer() :> ISerializer

        let comparer = new MeanComparer(spaceFactor = 0.2, leastAcceptableImprovementFactor = 1.)

        let tester = new ImplemantationComparer<_>(fsp, [bfs;ndc;jdn;pbn;ssj;sst], throwOnError = true, comparer = comparer)
        let tests = PerfTest.OfModuleMarker<PerformanceTests.Marker> ()

        override __.PerfTester = tester :> _
        override __.PerfTests = tests


    type ``Past FsPickler Versions Comparison`` () =
        inherit NUnitPerf<ISerializer> ()

        let persistResults = true
        let persistenceFile = "fspPerf.xml"

        let fsp = testSerializer :> ISerializer
        let version = typeof<FsPickler>.Assembly.GetName().Version
        let comparer = new MeanComparer(spaceFactor = 0.2, leastAcceptableImprovementFactor = 0.8)
        let tests = PerfTest.OfModuleMarker<PerformanceTests.Marker> ()
        let tester = 
            new PastImplementationComparer<ISerializer>(
                fsp, version, historyFile = persistenceFile, throwOnError = true, comparer = comparer)

        override __.PerfTester = tester :> _
        override __.PerfTests = tests

        [<TestFixtureTearDown>]
        member __.Persist() =
            if persistResults then tester.PersistCurrentResults ()
*)