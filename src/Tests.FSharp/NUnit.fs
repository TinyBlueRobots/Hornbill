[<AutoOpen>]
module NUnit

open NUnit.Framework

let private assrt assertion actual expected = assertion(box expected, box actual, sprintf "Expected: %+A%sActual: %+A" expected System.Environment.NewLine actual)
let (==) actual expected = assrt Assert.AreEqual actual expected 
let (!=) actual expected = assrt Assert.AreNotEqual actual expected 
 
type Test = TestAttribute

type SetUp = TestFixtureSetUpAttribute

type TearDown = TestFixtureTearDownAttribute

type Explicit = ExplicitAttribute